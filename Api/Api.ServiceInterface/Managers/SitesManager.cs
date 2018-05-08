using System;
using MongoDB.Bson;
using ServiceBelt;
using Dmo=Shared.DataModel;
using ToolBelt;
using System.Collections.Generic;
using TsonLibrary;
using System.IO;
using System.Linq;
using Rql.MongoDB;
using ServiceStack;
using System.Text.RegularExpressions;
using System.Text;
using MongoDB.Driver;

namespace Api.ServiceInterface
{
    public class SitesManager : ISitesManager
    {
        public IMongoManager Mongo { get; set; }

        readonly string titleRegexPattern = @"<h..+?>(?'title'.*?)</h.>";
        readonly string summaryRegexPattern = @"<p>(?'summary'.*?)</p>";
        readonly string imageRegexPattern = @"<img .*?src=""(?'imageUrl'/.*?)""";

        public async void UpdateArticleFiles(Dmo.Article article)
        {
            var site = await Mongo.GetCollection<Dmo.Site>().Find(s => s.Id == article.SiteId).FirstAsync();

            ParsedPath articlesPath;
            ParsedPath contentsPath;

            site.GetPaths(out articlesPath, out contentsPath);

            var contentColl = Mongo.GetCollection<Dmo.Content>();
            var html = await contentColl.Find(c => c.Id == article.HtmlId).FirstAsync();
            var images = await contentColl.Find(Builders<Dmo.Content>.Filter.In(c => c.Id, article.ImageIds)).ToListAsync();

            // Update the article and content
            UpdateArticleFiles(article, html, articlesPath);
            UpdateContentFiles(images, contentsPath);
        }

        public async void AddTitleSummaryAndImage(Dmo.Article article)
        {
            var content = await Mongo.GetCollection<Dmo.Content>().Find(c => c.Id == article.HtmlId).FirstAsync();
            string html = Encoding.UTF8.GetString(content.Data);
            Match m;

            if (String.IsNullOrEmpty(article.Title))
            {
                m = Regex.Match(html, titleRegexPattern, RegexOptions.Singleline);

                if (m.Success)
                {
                    article.Title = m.Groups["title"].Value;
                }
            }

            if (String.IsNullOrEmpty(article.Summary))
            {
                m = Regex.Match(html, summaryRegexPattern, RegexOptions.Multiline);

                if (m.Success)
                {
                    article.Summary = m.Groups["summary"].Value;
                }
            }

            if (String.IsNullOrEmpty(article.ImageUrl))
            {
                m = Regex.Match(html, imageRegexPattern, RegexOptions.Singleline);

                if (m.Success)
                {
                    article.ImageUrl = m.Groups["imageUrl"].Value;
                }
            }
        }

        public static void UpdateArticleFiles(Dmo.Article article, Dmo.Content html, ParsedPath articlesPath)
        {
            File.WriteAllBytes(articlesPath.WithFileAndExtension(article.Id.ToRqlId().ToString("n") + ".html"), html.Data);
        }

        public static void UpdateContentFiles(List<Dmo.Content> images, ParsedPath contentsPath)
        {
            foreach (var image in images)
            {
                var path = contentsPath.WithFileAndExtension(image.Id.ToRqlId().ToString("n") + MimeTypes.GetExtension(image.MimeType));

                File.WriteAllBytes(path, image.Data);
            }
        }
    }
}

