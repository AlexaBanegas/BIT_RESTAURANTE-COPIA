using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;

namespace BIT_PRUEBA_BE
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            var cors = new EnableCorsAttribute("*", "*", "*");
            config.EnableCors(cors);


            var jsonSettings = config.Formatters.JsonFormatter.SerializerSettings;
            jsonSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
            jsonSettings.Formatting = Newtonsoft.Json.Formatting.Indented;

            config.Formatters.Remove(config.Formatters.XmlFormatter);
        }
    }
}