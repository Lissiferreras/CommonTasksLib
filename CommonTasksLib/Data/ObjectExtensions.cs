﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommonTasksLib.Structs;

namespace CommonTasksLib.Data
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Metodo utilitario privado para traspasar los valores
        /// de las propiedades con igual nombre desde un objeto
        /// a otro.
        /// </summary>
        /// <param name="source">Instancia del objeto del cual se obtendrán los datos.</param>
        /// <param name="target">Instancia del objeto que recibirá los datos.</param>
        static void Transfer(object source, object target, List<string> toSkip = null)
        {
            var sourceType = source.GetType(); //tipo de objeto de instancia fuente
            var targetType = target.GetType(); //tipo de objeto de instancia destino

            //creación de parámetros para la expresión lambda
            var sourceParameter = Expression.Parameter(typeof(object), "source");
            var targetParameter = Expression.Parameter(typeof(object), "target");

            //creación de variables para la expresión lambda
            var sourceVariable = Expression.Variable(sourceType, "castedSource");
            var targetVariable = Expression.Variable(targetType, "castedTarget");

            var expressions = new List<Expression>();
            //agregar variables y parámetros a las expresiones lambda a ejecutar
            expressions.Add(Expression.Assign(sourceVariable, Expression.Convert(sourceParameter, sourceType)));
            expressions.Add(Expression.Assign(targetVariable, Expression.Convert(targetParameter, targetType)));

            foreach (var property in sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // verificar si la propiedad fuente admite lectura.
                if (!property.CanRead) 
                    continue;

                // verificar si la propiedad no debe ser transferida.
                if (toSkip != null)
                    if (toSkip.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                        continue;

                var targetProperty = targetType.GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance);
                if (targetProperty != null
                        && targetProperty.CanWrite //se puede escribir en la propiedad de destino?
                        && targetProperty.PropertyType.IsAssignableFrom(property.PropertyType))
                {
                    expressions.Add(
                        Expression.Assign( //expresión para la asignación de las propiedades de los objetos.
                            Expression.Property(targetVariable, targetProperty),
                            Expression.Convert(
                                    Expression.Property(sourceVariable, property), targetProperty.PropertyType)));
                }
            }

            // creación formal de la expresión lambda a ejecutar.
            var lambda =
                Expression.Lambda<Action<object, object>>(
                    Expression.Block(new[] { sourceVariable, targetVariable }, expressions),
                    new[] { sourceParameter, targetParameter });

            var del = lambda.Compile(); //compilar expresión lambda y obtener el delegado.

            del(source, target); //ejectuar la expresión lambda utilizando el delegado obtenido.
        }

        /// <summary>
        /// Metodo para copiar los datos de propiedades con igual nombre
        /// desde una instancia de una clase hacia otra.
        /// </summary>
        /// <typeparam name="SourceType">Tipo de datos del objeto fuente (proveedor de datos)</typeparam>
        /// <typeparam name="TargetType">Tipo de datos del objeto destino (receptor de datos)</typeparam>
        /// <param name="source">Instancia del objeto fuente de los datos.</param>
        /// <param name="targetObj">Instancia opcional del objeto recibidor de los datos</param>
        /// <returns></returns>
        public static void Transfer<SourceType, TargetType>(this SourceType source, ref TargetType targetObj, string toSkip = null)
            where TargetType : class, new()
             where SourceType: class
        {
            if (targetObj == null)
            {
                targetObj = new TargetType();
            }
            if (toSkip != null)
            {
                List<string> skipList = toSkip.Split(',').Where(s => !String.IsNullOrEmpty(s))
                    .Select(s => s.Trim()).ToList();
                Transfer(source, targetObj, skipList);
            }
            else
            {
                Transfer(source, targetObj);
            }
        }

        /// <summary>
        /// Método extensión para crear una cadena que contiene los valores de todas las propiedades
        /// públicas de una clase.
        /// </summary>
        /// <typeparam name="T">Tipo de datos de la clase en cuestión.</typeparam>
        /// <param name="source">Objeto de la instancia.</param>
        /// <param name="delimiter">Delimitador usado para separar las propiedades.</param>
        /// <returns>Objeto String que contiene las propiedades separadas con el separador.</returns>
        public static string ToString<T>(this T source, string delimiter = "\n")
            where T: class
        {
            string result = "";
            string format = "";
            var type = source.GetType();

            foreach (var property in type.GetProperties())
            {
                format += string.Format("{0}:{{{0}}}{1}", property.Name, delimiter);
            }
            format = format.TrimEnd(delimiter.ToArray<char>());
            result = source.FormatWith(format);

            return result;
        }

        /// <summary>
        /// Método extensión utilizado para obtener un atributo custom definido a una
        /// clase.
        /// </summary>
        /// <typeparam name="T">Tipo del atributo customizado.</typeparam>
        /// <param name="source">Instancia de la clase que se quiere obtener el atributo.</param>
        /// <returns>Una instancia del atributo definido para dicha clase, o NULL si no lo tiene definido.</returns>
        public static T GetCustomAttribute<T>(this object source)
            where T: class
        {
            var type = source.GetType();

            return type.GetCustomAttribute(typeof(T), true) as T;
        }

        /// <summary>
        /// Método extensión utilizado para convertir una instancia de un objeto a otra instancia del tipo
        /// destino.
        /// </summary>
        /// <typeparam name="U">Tipo de datos del objeto destino</typeparam>
        /// <param name="source">Instancia del objeto a convertir.</param>
        /// <returns>Instancia del objeto convertida al tipo U especificado.</returns>
        public static U ConvertTo<U>(this object source)
        {
            try
            {
                Type conversionType = Nullable.GetUnderlyingType(typeof(U)) ?? typeof(U);
                return (U)Convert.ChangeType(source, conversionType);
            }
            catch
            {
                return default(U);
            }
        }
    }
}
