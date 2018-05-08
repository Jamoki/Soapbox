using System;
using Dmo=Shared.DataModel;

namespace Api.ServiceInterface
{
    public interface ISitesManager
    {
        void UpdateArticleFiles(Dmo.Article article);
        void AddTitleSummaryAndImage(Dmo.Article article);
    }
}

