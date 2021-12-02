using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace PeD.Core.Models
{
    public class LogItem
    {
        public string Titulo { get; set; }
        public object Valor { get; set; }
        public string Type { get; set; }

        [JsonConstructor]
        public LogItem(string Titulo, object Valor)
        {
            this.Titulo = Titulo;
            this.Valor = Valor;
            if (Valor != null)
                Type = Valor.GetType().FullName;
        }

        public LogItem(string Titulo, object Valor, string Type)
        {
            this.Titulo = Titulo;
            this.Valor = Valor;
            this.Type = Type;
        }

        public static List<LogItem> GerarItems(object Entity, object EntityOld = null)
        {
            var logItems = new List<LogItem>();

            if (Entity != null)
            {
                var props = Entity.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var property in props)
                {
                    object oldValue = null;
                    var logger = (Attributes.LoggerAttribute)property
                        .GetCustomAttributes(typeof(Attributes.LoggerAttribute), false).FirstOrDefault();

                    if (logger is null)
                        continue;
                    var propertyValue = property.GetValue(Entity);

                    if (EntityOld != null)
                    {
                        oldValue = property.GetValue(EntityOld);
                    }

                    if (propertyValue != null && !propertyValue.Equals(oldValue))
                    {
                        var type = propertyValue.GetType();

                        if (type.GetInterfaces().Any(t => t == typeof(IEnumerable)) && type.IsGenericType)
                        {
                            foreach (var item in (IEnumerable)propertyValue)
                            {
                                var tempLogItens = GerarItems(item);
                                if (tempLogItens.Count > 0)
                                {
                                    logItems.AddRange(tempLogItens);
                                }
                            }
                        }
                        else
                        {
                            var showValue = logger.hasValueFrom
                                ? Entity.GetPropValue(logger.ValueFrom)
                                : propertyValue;
                            var showName = logger.Name ?? property.Name;
                            logItems.Add(new LogItem(showName, showValue));
                        }
                    }
                }
            }

            return logItems;
        }
    }

    internal static class ExtensionObject
    {
        public static object GetPropValue(this object obj, string propName)
        {
            var nameParts = propName.Split('.');
            if (nameParts.Length == 1)
            {
                return obj.GetType().GetProperty(propName)?.GetValue(obj, null);
            }

            foreach (var part in nameParts)
            {
                if (obj == null)
                {
                    return null;
                }

                var type = obj.GetType();
                var info = type.GetProperty(part);
                if (info == null)
                {
                    return null;
                }

                obj = info.GetValue(obj, null);
            }

            return obj;
        }
    }
}