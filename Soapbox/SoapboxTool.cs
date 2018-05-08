using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using ToolBelt;
using ServiceBelt;
using Smo=Api.ServiceModel;
using ServiceStack.Auth;
using Rql;
using ServiceStack;
using TsonLibrary;

namespace Soapbox
{
    [CommandLineTitle("Soapbox Article Tool")]
    [CommandLineDescription("Manages articles")]
    [CommandLineCopyright("Copyright (c) 2015, Jamoki")]
    [CommandLineCommandDescription("list", Description = "Lists all sites in your config file")]
    [CommandLineCommandDescription("push", Description = "Pushes a new article out")]
    [CommandLineCommandDescription("pull", Description = "Pulls an old article down")]
    [CommandLineCommandDescription("hide", Description = "Hides an article")]
    [CommandLineCommandDescription("show", Description = "Shows an article")]
    [CommandLineCommandDescription("delete", Description = "Updates an existing article")]
    [CommandLineCommandDescription("help", Description = "Displays help for this tool ")]
    public class SoapboxTool : ToolBase
    {
        class ConfigNode : TsonTypedObjectNode
        {
            public TsonArrayNode<SiteNode> Sites { get; set; }
        }

        class SiteNode : TsonTypedObjectNode
        {
            [TsonNotNull]
            public TsonStringNode ServiceUrl { get; set; }
            [TsonNotNull]
            public TsonStringNode SiteId { get; set; }
            [TsonNotNull]
            public TsonStringNode Name { get; set; }
            [TsonNotNull]
            public TsonStringNode ShortName { get; set; }
            [TsonNotNull]
            public TsonStringNode RootUrl { get; set; }
            public TsonStringNode Email { get; set; }
            public TsonStringNode Password { get; set; }
        }

        [CommandLineArgument("help", ShortName="?", Description="Shows this help", Commands="push,pull,hide,show,delete,help")]
        public bool ShowUsage { get; set; }

        [CommandCommandLineArgument(Description="One of new, delete, update or help", Commands="push,pull,hide,show,delete,help")]
        public string Command { get; set; }

        [CommandLineArgument("service", ShortName="u", Description="Service URL, e.g. https://api.yourdomain.com/soapbox/v1", 
            Commands="push,pull,hide,show,delete,list")]
        public ParsedUrl ServiceUrl { get; set; }

        [CommandLineArgument("tags", ShortName="t", Description="Tags to set for the article, e.g. drama,poetry,code", Commands="push")]
        public List<string> Tags { get; set; }

        [CommandLineArgument("site", ShortName="s", Description="Site name, either the short name or the full name.", Commands="push,pull,list,delete")]
        public string SiteName { get; set; }

        [CommandLineArgument("email", ShortName="e", Description="Email of the site user.", Commands="push,pull")]
        public ParsedEmail Email { get; set; }

        [CommandLineArgument("password", ShortName="p", Description="Password of the site user.", Commands="push,pull")]
        public string Password { get; set; }

        [CommandLineArgument("config", ShortName="c", Description="Alternative configuration file path.  Default is ~/.soapbox/config", Commands="push,pull")]
        public ParsedFilePath ConfigPath { get; set; }

        [CommandLineArgument("article", ShortName="a", Description="Article ID", Commands="pull,delete")]
        public RqlId ArticleId { get; set; }

        [DefaultCommandLineArgument(Description = "Command or file name", Commands="help, push")]
        public string Default { get; set; }

        public override void Execute()
        {
            WriteMessage(this.Parser.LogoBanner);

            if (ShowUsage || String.IsNullOrEmpty(this.Command) || this.Command == "help")
            {
                if (this.Command != "help")
                    WriteMessage(this.Parser.Usage);
                else
                    WriteMessage(this.Parser.GetUsage(Default));

                return;
            }

            if (Tags == null)
                Tags = new List<string>();

            if (ConfigPath == null)
            {
                ConfigPath = new ParsedFilePath(
                    new ParsedPath(Environment.GetEnvironmentVariable("HOME"), PathType.Directory).Append(".soapbox/config", PathType.File));
            }

            if (!File.Exists(ConfigPath))
            {
                WriteError("No '{0}' file found. Please it create with at least one site.");
                return;
            }

            var configNode = Tson.ToObjectNode<ConfigNode>(File.ReadAllText(ConfigPath));

            if (Command == "list" && String.IsNullOrEmpty(SiteName))
            {
                ListSites(configNode);
                return;
            }

            SiteNode siteNode = null;

            if (configNode.Sites.Count == 1)
            {
                siteNode = configNode.Sites[0];
            }
            else
            {
                if (String.IsNullOrEmpty(SiteName))
                {
                    WriteError("No site name given and there are multiple. Use 'list' command to see available sites.");
                    return;
                }

                siteNode = configNode.Sites.FirstOrDefault(s => s.ShortName.Value == SiteName || s.Name.Value == SiteName);

                if (siteNode == null)
                {
                    WriteError("Site '{0}' was not found in config file", SiteName);
                    return;
                }
            }

            if (ServiceUrl == null)
            {
                if (siteNode.ServiceUrl == null)
                {
                    WriteError("A service URL must be given either in the config file or on the command line");
                    return;
                }

                ServiceUrl = new ParsedUrl(siteNode.ServiceUrl.Value);
            }

            if (Email == null)
            {
                if (siteNode.Email == null)
                {
                    WriteError("An email must be give either in the config file or on the command line");
                    return;
                }

                Email = new ParsedEmail(siteNode.Email.Value);
            }

            if (Password == null)
            {
                if (siteNode.Password == null)
                {
                    WriteError("A password must be give either in the config file or on the command line");
                    return;
                }

                Password = siteNode.Password.Value;
            }

            var rootUrl = new ParsedUrl(siteNode.RootUrl.Value);
            var siteId = new RqlId(siteNode.SiteId.Value);

            switch (Command)
            {
            case "list":
                ListArticles(siteId);
                break;
            case "push":
                PushArticle(rootUrl, siteId);
                break;
            case "delete":
                DeleteArticle(rootUrl, siteId);
                break;
            }
        }

        void ListSites(ConfigNode configNode)
        {
            foreach (var siteNode in configNode.Sites)
            {
                Console.WriteLine("Site Name: '{0}' Short Name: '{1}', Id: '{2}'", 
                    siteNode.Name.Value, siteNode.ShortName.Value, siteNode.SiteId.Value);
            }
        }

        void ListArticles(RqlId siteId)
        {
            var client = new JsonServiceClient(ServiceUrl);
            var loginResponse = client.Post<Smo.RequestLoginResponse>(new Smo.RequestLogin 
                { Email = Email.UserAndHost, Password = Password });

            client.Headers["Authorization"] = "Bearer " + loginResponse.LoginToken;

            var response = client.Get<ListResponse<Smo.Article>>("/data/articles");

            foreach (var item in response.Items)
            {
                Console.WriteLine("Article Id: {0}", item.Id.ToString());
            }

            client.Delete(new Smo.Login());
        }

        void DeleteArticle(ParsedUrl rootUrl, RqlId siteId)
        {
            var client = new JsonServiceClient(ServiceUrl);
            var loginResponse = client.Post<Smo.RequestLoginResponse>(new Smo.RequestLogin 
                { Email = Email.UserAndHost, Password = Password });

            client.Headers["Authorization"] = "Bearer " + loginResponse.LoginToken;

            client.Delete("/data/articles/" + ArticleId);
            client.Delete(new Smo.Login());

            WriteMessage("Article '{0}' deleted", ArticleId);
        }

        ParsedPath GetWorkingDir()
        {
            var root = new ParsedDirectoryPath(Path.GetTempPath());
            ParsedPath path = null;

            for(;;)
            {
                path = root.Append(Base62KeyGenerator.Generate(12), PathType.Directory);

                try
                {
                    Directory.CreateDirectory(path);
                    break;
                }
                catch (IOException)
                {
                }
            }

            return path;
        }

        string FindFileLinks(string markdown, out Dictionary<string, ParsedUrl> fileLinks)
        {
            var regex = new Regex(@"(?'file'file://[/0-9a-zA-Z\-\.]+)", RegexOptions.ExplicitCapture);
            var index = 0;
            var forward = new Dictionary<string, ParsedUrl>();
            var reverse = new Dictionary<ParsedUrl, string>();

            var s = regex.Replace(markdown, m => 
            {
                var link = new ParsedUrl(m.Groups[0].Value);
                string key;

                if (!reverse.TryGetValue(link, out key))
                {
                    key = (index++).ToString();
                    forward.Add(key, link);
                    reverse.Add(link, key);
                }

                return "{{" + key + "}}";
            });

            fileLinks = forward;

            return s;
        }

        void PushArticle(ParsedUrl rootUrl, RqlId siteId)
        {
            // Create a temp directory to work in
            var workingDir = GetWorkingDir();
            var client = new JsonServiceClient(ServiceUrl);

            var loginResponse = client.Post<Smo.RequestLoginResponse>(new Smo.RequestLogin { Email = Email.UserAndHost, Password = Password });

            client.Headers["Authorization"] = "Bearer " + loginResponse.LoginToken;

            var imageIds = new List<RqlId>();
            RqlId? markdownId = null;
            RqlId? htmlId = null;
            bool published = false;

            try
            {
                var markdownPath = workingDir.WithFileAndExtension(new ParsedFilePath(Default).FileAndExtension);
                var markdown = File.ReadAllText(Default);
                var markdownTool = "/usr/local/bin/multimarkdown";
                var htmlPath = markdownPath.WithExtension(".html");
                string output;
                Dictionary<string, ParsedUrl> fileLinks;

                // Identify all the images in the Markdown, replacing with the publish URL
                markdown = FindFileLinks(markdown, out fileLinks);

                var newFileLinks = new Dictionary<string, string>();

                // Check all the files exist and modify all links to point to the published content
                foreach (var fileLink in fileLinks)
                {
                    var fileName = fileLink.Value.Path;

                    if (!File.Exists(fileName))
                    {
                        WriteError("File '{0}' does not exist", fileLink);
                        return;
                    }

                    var fileInfo = new FileInfo(fileName);
                    var maxFileLengthMegabytes = 8;

                    if (fileInfo.Length > maxFileLengthMegabytes * 1024 * 1024)
                    {
                        WriteError("File '{0}' must be less than {1} Mbytes", maxFileLengthMegabytes);
                        return;
                    }

                    // Upload each of the images
                    var imageResponse = client.PostFile<PostResponse>("/data/contents", fileInfo, 
                        MimeTypes.GetMimeType(fileInfo.FullName));

                    // Record the id
                    imageIds.Add(imageResponse.Id);

                    // Update the link entry
                    newFileLinks.Add(fileLink.Key, 
                        "/contents/" + imageResponse.Id.ToString("n") + Path.GetExtension(fileInfo.FullName));
                }

                // Update the links in the markdown
                markdown = StringUtility.ReplaceTags(markdown, "{{", "}}", newFileLinks);

                // Write the modified markdown out
                File.WriteAllText(markdownPath, markdown);

                // Process the markdown file into HTML
                ToolBelt.Command.Run("cd {0}; {1} {2} -o {3}".InvariantFormat(workingDir, markdownTool, markdownPath, htmlPath), out output);

                if (!String.IsNullOrEmpty(output) || !File.Exists(htmlPath))
                {
                    WriteError("Unable to create HTML from Markdown: {0}", output);
                    // TODO: Delete all uploaded content
                    return;
                }

                // Upload the Markdown & HTML
                var markdownResponse = client.PostFile<PostResponse>("/data/contents", new FileInfo(markdownPath), MimeTypes.GetMimeType(markdownPath));

                markdownId = markdownResponse.Id;

                var htmlResponse = client.PostFile<PostResponse>("/data/contents", new FileInfo(htmlPath), MimeTypes.GetMimeType(htmlPath));

                htmlId = htmlResponse.Id;

                // Create the Article entry
                var articleResponse = client.Post<PostResponse>("/data/articles", new Smo.Article
                {
                    BodyId = markdownResponse.Id,
                    HtmlId = htmlResponse.Id,
                    ImageIds = imageIds,
                    SiteId = siteId,
                    Tags = this.Tags
                });

                published = true;

                var articleUrl = new ParsedUrl(rootUrl + "/article/" + articleResponse.Id.ToString("n"));

                Console.WriteLine("Article '{0}' published", articleUrl);
            }
            finally
            {
                // Clean up the temp directory
                Directory.Delete(workingDir, true);

                if (!published)
                {
                    // Clean up uploaded images
                    foreach (var imageId in imageIds)
                    {
                        client.Delete("/data/contents/" + imageId.ToString());
                    }

                    if (markdownId.HasValue)
                    {
                        client.Delete("/data/contents/" + markdownId.Value.ToString());
                    }

                    if (htmlId.HasValue)
                    {
                        client.Delete("/data/contents/" + htmlId.Value.ToString());
                    }
                }

                client.Delete(new Smo.Login());
            }
        }
    }
}
