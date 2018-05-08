using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using ToolBelt;
using MongoDB.Driver;
using System.Text;

namespace Backlog
{
    [CommandLineCopyright("Copyright (c) 2015, Jamoki")]
    [CommandLineDescription("A tool for backing up the database")]
    [CommandLineTitle("Backup Utility")]
    public class BackupDbTool : ToolBase
    {
        private class S3Object
        {
            public DateTime Timestamp { get; set; }
            public long SizeInBytes { get; set; }
            public string Name { get; set; }
        }

        private string awsPath;
        private string mongoDumpPath;
        private string tarPath;
        private string curlPath;

        [CommandLineArgument("help", ShortName = "?", Description = "Show this help")]
        public bool ShowHelp { get; set; }

        [AppSettingsArgument]
        [CommandLineArgument("profile", ShortName = "p", Description = "AWS profile name")]
        public string AwsProfile { get; set; }

        [AppSettingsArgument]
        [CommandLineArgument("bucket", ShortName = "b", Description = "S3 backup bucket name")]
        public string BucketName { get; set; }

        [AppSettingsArgument]
        [CommandLineArgument("max", ShortName = "x", Description = "Maximum number of backups to keep")]
        public int? MaxBackups { get; set; }

        [AppSettingsArgument]
        [CommandLineArgument("emailto", ShortName = "e", Description = "One or more people to send e-mail too")]
        public List<string> EMailTo { get; set; }

        [AppSettingsArgument]
        [CommandLineArgument("ec2system", Description = "Program is running on an EC2 system")]
        public bool OnEC2System { get; set; }

        [AppSettingsArgument]
        [CommandLineArgument("mongo", ShortName="m", Description = "Database to backup", ValueHint = "MONGO_URL")]
        public MongoUrl MongoUrl { get; set; }

        public BackupDbTool()
        {
            EMailTo = new List<string>();
        }

        public override void Execute()
        {
            // TODO: Put on command line?
            bool debug = false;

            if (ShowHelp)
            {
                WriteMessage(Parser.LogoBanner);
                WriteMessage(Parser.Usage);
                return;
            }

            if (String.IsNullOrEmpty(this.AwsProfile))
            {
                WriteError("Must specify an AWS profile listed in the ~/.aws/config file");
                return;
            }

            if (String.IsNullOrEmpty(this.BucketName))
            {
                WriteError("Must specify the name of an existing S3 bucket");
                return;
            }

            mongoDumpPath = FindTool("mongodump", new string[] { "/usr/bin/mongodump", "/usr/local/bin/mongodump" });
            tarPath = FindTool("tar", new string[] {"/bin/tar", "/usr/bin/tar" });
            awsPath = FindTool("aws", new string[] { "/usr/local/bin/aws" });
            curlPath = FindTool("curl", new string[] { "/usr/bin/curl" });

            ParsedPath dumpDir = null;

            if (!MaxBackups.HasValue) 
            {
                MaxBackups = 10;
                WriteWarning("Using a max backups of {0}", MaxBackups);
            }

            var s3Objs = new List<S3Object>();
            string output;
            string command = "{0} s3 ls s3://{1} --profile {2}".InvariantFormat(awsPath, BucketName, AwsProfile);
            int exitCode = Command.Run(command, out output);

            if (exitCode != 0)
            {
                WriteError("Unable to list existing objects in bucket '{0}'.", BucketName);
                return;
            }

            using (StringReader reader = new StringReader(output))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    string[] split = line.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);

                    S3Object s3Obj = new S3Object();

                    s3Obj.Timestamp = DateTime.Parse(split[0] + " " + split[1]);
                    s3Obj.SizeInBytes = long.Parse(split[2]);
                    s3Obj.Name = split[3];

                    s3Objs.Add(s3Obj);
                }
            }

            try
            {
                WriteMessage("Backing up {0}", MongoUrl);

                if (String.IsNullOrEmpty(MongoUrl.DatabaseName))
                {
                    WriteError("URL {0} does not contain a database");
                    return;
                }

                DateTime now = DateTime.UtcNow;
                var dbDns = Dns.GetHostEntry(Dns.GetHostAddresses(MongoUrl.Server.Host)[0]).AddressList[0].ToString();
                string hostName = Dns.GetHostName();
                bool isLocalHost = 
                    Dns.GetHostEntry("localhost").AddressList[0].ToString() == dbDns ||
                    Dns.GetHostEntry(hostName).AddressList[0].ToString() == dbDns;

                if (isLocalHost)
                {
                    if (OnEC2System)
                    {
                        command = "{0} --max-time 3 --silent http://169.254.169.254/latest/meta-data/public-hostname".InvariantFormat(curlPath);
                        WriteMessage(command);
                        exitCode = Command.Run(command, out output);

                        if (exitCode == 0)
                        {
                            hostName = output.Trim(' ', '\t', '\n');
                        }
                    }
                }
                else
                {
                    hostName = MongoUrl.Server.Host;
                }

                WriteMessage("Database host is '{0}'.", hostName);

                dumpDir = new ParsedPath(Path.GetTempPath(), PathType.Directory).Append(
                    hostName + "-" + now.Ticks.ToString(), PathType.Directory);

                if (Directory.Exists(dumpDir))
                    Directory.Delete(dumpDir, recursive: true);

                Directory.CreateDirectory(dumpDir);

                if (String.IsNullOrEmpty(MongoUrl.Username))
                {
                    command = "{0} -h {1}:{2} -d {3} -o {4}".InvariantFormat(
                        mongoDumpPath, 
                        MongoUrl.Server.Host,
                        MongoUrl.Server.Port,
                        MongoUrl.DatabaseName,
                        dumpDir);
                }
                else
                {
                    command = "{0} -u {1} -p '{2}' -h {3}:{4} -d {5} -o {6}".InvariantFormat(
                        mongoDumpPath, 
                        MongoUrl.Username, 
                        MongoUrl.Password, 
                        MongoUrl.Server.Host,
                        MongoUrl.Server.Port,
                        MongoUrl.DatabaseName,
                        dumpDir);
                }
                WriteMessage(command);

                exitCode = Command.Run(command, out output);

                if (exitCode != 0)
                {
                    WriteError("Unable to backup database:\n{0}", output);
                    return;
                }

                string hostAndDatabase = "{0}-{1}".InvariantFormat(hostName, MongoUrl.DatabaseName);

                ParsedPath backupFileName = new ParsedPath("{0}-{1}.tar.gz".InvariantFormat(
                    hostAndDatabase,
                    now.ToString("yyyyMMdd'-'HHmmss'Z'")), PathType.File);

                command = "cd {0}; {1} -czvf {2} {3}/*".InvariantFormat(
                    dumpDir,   
                    tarPath, 
                    backupFileName,
                    MongoUrl.DatabaseName);

                WriteMessage(command);

                exitCode = Command.Run(command, out output);

                if (exitCode != 0)
                {
                    WriteError("Unable to create database tar file:\n{0}", output);
                    return;
                }

                long backupFileSize = new FileInfo(dumpDir.Append(backupFileName)).Length;

                var existingS3Objs = s3Objs.Where(o => o.Name.StartsWith(hostAndDatabase)).ToList();

                existingS3Objs.Sort((x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name));

                while (existingS3Objs.Count > MaxBackups - 1)
                {
                    S3Object s3Obj = existingS3Objs[0];
                    command = "{0} s3 rm s3://{1}/{2} --profile {3}".InvariantFormat(
                        awsPath, BucketName, s3Obj.Name, AwsProfile);
                    
                    WriteMessage(command); 
                    exitCode = Command.Run(command, out output);

                    if (exitCode != 0)
                    {
                        WriteError("Unable to remove old backups file '{0}'", s3Obj.Name);
                        return;
                    }
                    existingS3Objs.RemoveAt(0);
                }

                command = "cd {0}; {1} s3 cp {2} s3://{3}/ --profile {4}".InvariantFormat(
                    dumpDir, awsPath, backupFileName, BucketName, AwsProfile);
                WriteMessage(command);
                exitCode = Command.Run(command, out output, debug);
                if (exitCode != 0)
                {
                    WriteError("Unable to upload backup to bucket '{0}'", BucketName);
                    return;
                }

                WriteMessage("rm -rf {0}", dumpDir);
                Directory.Delete(dumpDir, recursive: true);
                dumpDir = null;

                string messageText = GetEmbeddedResource("MongoBackupReport.md");
                string messageHtml = GetEmbeddedResource("MongoBackupReport.html");

                var dict = new Dictionary<string, string>();

                dict["HostName"] = MongoUrl.Server.Host;
                dict["Port"] = MongoUrl.Server.Port.ToString();
                dict["Database"] = MongoUrl.DatabaseName;
                dict["DateTime"] = now.ToString();
                dict["FileName"] = backupFileName;
                dict["FileSize"] = backupFileSize.ToString();
                dict["NumBackups"] = (existingS3Objs.Count + 1).ToString();
                dict["S3Url"] = "http://{0}.s3.amazonaws.com/{1}".InvariantFormat(BucketName, backupFileName);
                dict["BackupHostname"] = hostName;

                messageText = messageText.ReplaceTags("{{", "}}", dict, TaggedStringOptions.LeaveUnknownTags);
                messageHtml = messageHtml.ReplaceTags("{{", "}}", dict, TaggedStringOptions.LeaveUnknownTags);

                ParsedPath messageFile = new ParsedPath(Path.GetTempPath(), PathType.Directory).WithFileAndExtension("BackupMessage.json");
                string messageJson = GetEmbeddedResource(messageFile.FileAndExtension);

                dict.Clear();
                dict["BackupMessageText"] = EscapeJsonString(messageText);
                dict["BackupMessageHtml"] = EscapeJsonString(messageHtml);

                messageJson = messageJson.ReplaceTags("{{", "}}", dict, TaggedStringOptions.ThrowOnUnknownTags);

                File.WriteAllText(messageFile, messageJson);

                ParsedPath destFile = new ParsedPath(Path.GetTempPath(), PathType.Directory).WithFileAndExtension("BackupEmailDestinations.json");
                string destJson = GetEmbeddedResource(destFile.FileAndExtension);

                dict.Clear();
                dict["EMailToList"] = StringUtility.Join(", ", EMailTo.Select(e => "\"" + EscapeJsonString(e) + "\"").ToList());

                destJson = destJson.ReplaceTags("{{", "}}", dict, TaggedStringOptions.ThrowOnUnknownTags);

                File.WriteAllText(destFile, destJson);

                // TODO: Needs to come from command line
                string region = "us-west-2";

                command = "{0} ses send-email --region {1} --from {2} --destination file://{3} --message file://{4} --profile {5}".InvariantFormat(
                    awsPath, region, "support@jamoki.com", destFile, messageFile, AwsProfile);
                
                WriteMessage(command);

                exitCode = Command.Run(command, out output);

                if (exitCode != 0)
                {
                    WriteWarning("Unable to send e-mail:\n{0}", output);
                }

                File.Delete(destFile);
                File.Delete(messageFile);
                WriteMessage("exit");
            }
            catch (Exception e)
            {
                WriteError("Unable to complete backup\n{0}", e.ToString());
            }
            finally
            {
                if (dumpDir != null && Directory.Exists(dumpDir))
                {
                    Directory.Delete(dumpDir, recursive: true);
                }
            }
        }

        private string FindTool(string toolName, string[] paths)
        {
            string toolPath = paths.FirstOrDefault(p => File.Exists(p));

            if (toolPath == null)
            {
                throw new FileNotFoundException("Unable to find '{0}' tool at in paths {1}".CultureFormat(toolName, StringUtility.Join(PathUtility.PathSeparator, paths)));
            }

            return toolPath;
        }

        private string EscapeJsonString(string raw)
        {
            var sb = new StringBuilder(raw);
            sb.Replace("\\", "\\\\");
            sb.Replace("\"", "\\\"");
            sb.Replace("/", "\\/");
            sb.Replace("\t", " ");
            sb.Replace(Environment.NewLine, "\\n");
            return sb.ToString();
        }

        private string GetEmbeddedResource(string name)
        {
            string s;

            using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Backlog." + name)))
            {
                s = reader.ReadToEnd();
            }

            return s;
        }
    }
}
