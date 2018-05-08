using System;
using System.Net;
using Api.ServiceInterface;
using MongoDB.Bson;
using MongoDB.Driver;
using Rql;
using Rql.MongoDB;
using ServiceStack;
using ServiceBelt;
using Dmo = Shared.DataModel;
using Smo = Api.ServiceModel;
using System.IO;
using System.Text;

namespace Api.ServiceInterface
{
    public class ContentService : MongoService<Smo.Content, Smo.ContentQuery, Dmo.Content>
    {
        public override void BeforeValidation(Dmo.Content dmo)
        {
            if (Request.Files.Length > 0)
            {
                var file = Request.Files[0];

                using (var stream = new MemoryStream())
                {
                    file.InputStream.CopyTo(stream);

                    dmo.ByteLength = (int)stream.Length;
                    dmo.Data = stream.GetBuffer();
                    dmo.MimeType = file.ContentType;
                }
            }
        }
    }
}

