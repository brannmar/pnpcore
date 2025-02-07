﻿using Microsoft.SharePoint.Client;
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace PnP.Core.Transformation.SharePoint.Extensions
{
    /// <summary>
    /// Class for client object extension methods
    /// </summary>
    internal static class ClientObjectExtensions
    {
        /// <summary>
        /// Checks if the ClientObject is null
        /// </summary>
        /// <typeparam name="T">Type of object to operate on</typeparam>
        /// <param name="clientObject">Object to operate on</param>
        /// <returns>True if the server object is null, otherwise false</returns>
        public static bool ServerObjectIsNull<T>(this T clientObject) where T : ClientObject
        {
            if (clientObject == null)
            {
                return true;
            }
            return (!(clientObject.ServerObjectIsNull != null &&
            clientObject.ServerObjectIsNull.HasValue &&
            !clientObject.ServerObjectIsNull.Value));
        }

        /// <summary>
        /// Check if a property is available on a object
        /// </summary>
        /// <typeparam name="T">Type of object to operate on</typeparam>
        /// <param name="clientObject">Object to operate on</param>
        /// <param name="propertySelector">Lamda expression containing the properties to check (e.g. w => w.HasUniqueRoleAssignments)</param>
        /// <returns>True if the property is available, false otherwise</returns>
        public static bool IsPropertyAvailable<T>(this T clientObject, Expression<Func<T, object>> propertySelector) where T : ClientObject
        {
            var body = propertySelector.Body as MemberExpression ?? ((UnaryExpression)propertySelector.Body).Operand as MemberExpression;

            return clientObject.IsPropertyAvailable(body.Member.Name);
        }

        /// <summary>
        /// Check if a property is instantiated on a object
        /// </summary>
        /// <typeparam name="T">Type of object to operate on</typeparam>
        /// <param name="clientObject">Object to operate on</param>
        /// <param name="propertySelector">Lamda expression containing the properties to check (e.g. w => w.HasUniqueRoleAssignments)</param>
        /// <returns>True if the property is instantiated, false otherwise</returns>
        public static bool IsObjectPropertyInstantiated<T>(this T clientObject, Expression<Func<T, object>> propertySelector) where T : ClientObject
        {
            var body = propertySelector.Body as MemberExpression ?? ((UnaryExpression)propertySelector.Body).Operand as MemberExpression;

            return clientObject.IsObjectPropertyInstantiated(body.Member.Name);
        }

        /// <summary>
        /// Ensures that particular property is loaded on the <see cref="ClientObject"/> and immediately returns this property
        /// </summary>
        /// <typeparam name="T"><see cref="ClientObject"/> type</typeparam>
        /// <typeparam name="TResult">Property type</typeparam>
        /// <param name="clientObject"><see cref="ClientObject"/></param>
        /// <param name="propertySelector">Lamda expression containing the property to ensure (e.g. w => w.HasUniqueRoleAssignments)</param>
        /// <returns>Property value</returns>
        public static TResult EnsureProperty<T, TResult>(this T clientObject, Expression<Func<T, TResult>> propertySelector) where T : ClientObject
        {
            return Task.Run(() => EnsurePropertyImplementation(clientObject, propertySelector)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Ensures that particular property is loaded on the <see cref="ClientObject"/> and immediately returns this property
        /// </summary>
        /// <typeparam name="T"><see cref="ClientObject"/> type</typeparam>
        /// <typeparam name="TResult">Property type</typeparam>
        /// <param name="clientObject"><see cref="ClientObject"/></param>
        /// <param name="propertySelector">Lamda expression containing the property to ensure (e.g. w => w.HasUniqueRoleAssignments)</param>
        /// <returns>Property value</returns>
        public static async Task<TResult> EnsurePropertyAsync<T, TResult>(this T clientObject, Expression<Func<T, TResult>> propertySelector) where T : ClientObject
        {
            return await EnsurePropertyImplementation(clientObject, propertySelector).ConfigureAwait(false);
        }

        private async static Task<TResult> EnsurePropertyImplementation<T, TResult>(T clientObject, Expression<Func<T, TResult>> propertySelector) where T : ClientObject
        {
            if (propertySelector.Body.NodeType == ExpressionType.Call && propertySelector.Body is MethodCallExpression)
            {
                var body = (MethodCallExpression)propertySelector.Body;
                if (body.Method.IsGenericMethod &&
                    body.Method.DeclaringType == typeof(ClientObjectQueryableExtension) &&
                    (body.Method.Name == "Include" || body.Method.Name == "IncludeWithDefaultProperties"))
                {
                    if (body.Arguments.Count != 2)
                    {
                        throw new Exception("Invalid arguments number");
                    }

                    clientObject.Context.Load(clientObject, propertySelector.ToUntypedStaticMethodCallExpression());
                    await clientObject.Context.ExecuteQueryRetryAsync().ConfigureAwait(false);

                    var arg = (MemberExpression)(body.Arguments[0]);
                    var prop = (PropertyInfo)(Expression.Property(Expression.Constant(clientObject), arg.Member.Name).Member);

                    return (TResult)prop.GetValue(clientObject);
                }
                throw new Exception("Only 'Include' and 'IncludeWithDefaultProperties' methods are supported.");
            }

            var untypedExpresssion = propertySelector.ToUntypedPropertyExpression();
            if (!clientObject.IsPropertyAvailable(untypedExpresssion) && !clientObject.IsObjectPropertyInstantiated(untypedExpresssion))
            {
                clientObject.Context.Load(clientObject, untypedExpresssion);
                await clientObject.Context.ExecuteQueryRetryAsync().ConfigureAwait(false);
            }
            else if (clientObject.IsObjectPropertyInstantiated(untypedExpresssion))
            {
                if (EnsureCollectionLoaded(clientObject, untypedExpresssion))
                {
                    await clientObject.Context.ExecuteQueryRetryAsync().ConfigureAwait(false);
                }
            }

            return (propertySelector.Compile())(clientObject);
        }

        /// <summary>
        /// Ensures that particular properties are loaded on the <see cref="ClientObject"/> 
        /// </summary>
        /// <typeparam name="T"><see cref="ClientObject"/> type</typeparam>
        /// <param name="clientObject"><see cref="ClientObject"/></param>
        /// <param name="propertySelector">Lamda expressions containing the properties to ensure (e.g. w => w.HasUniqueRoleAssignments, w => w.ServerRelativeUrl)</param>
        /// <returns>Property value</returns>
        public static void EnsureProperties<T>(this T clientObject, params Expression<Func<T, object>>[] propertySelector) where T : ClientObject
        {
            Task.Run(() => EnsurePropertiesImplementation(clientObject, propertySelector)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Ensures that particular properties are loaded on the <see cref="ClientObject"/> 
        /// </summary>
        /// <typeparam name="T"><see cref="ClientObject"/> type</typeparam>
        /// <param name="clientObject"><see cref="ClientObject"/></param>
        /// <param name="propertySelector">Lamda expressions containing the properties to ensure (e.g. w => w.HasUniqueRoleAssignments, w => w.ServerRelativeUrl)</param>
        /// <returns>Property value</returns>
        public static async Task EnsurePropertiesAsync<T>(this T clientObject, params Expression<Func<T, object>>[] propertySelector) where T : ClientObject
        {
            await EnsurePropertiesImplementation(clientObject, propertySelector).ConfigureAwait(false);
        }
        internal static async Task EnsurePropertiesImplementation<T>(this T clientObject, params Expression<Func<T, object>>[] propertySelector) where T : ClientObject
        {
            var dirty = false;
            foreach (Expression<Func<T, object>> expression in propertySelector)
            {
                if (expression.Body.NodeType == ExpressionType.Call && expression.Body is MethodCallExpression)
                {
                    var body = (MethodCallExpression)expression.Body;
                    if (body.Method.IsGenericMethod &&
                        body.Method.DeclaringType == typeof(ClientObjectQueryableExtension) &&
                        (body.Method.Name == "Include" || body.Method.Name == "IncludeWithDefaultProperties"))
                    {
                        if (body.Arguments.Count != 2)
                        {
                            throw new Exception("Invalid arguments number");
                        }

                        if (!(clientObject is ClientObjectCollection) || ((clientObject is ClientObjectCollection) && !(clientObject as ClientObjectCollection).AreItemsAvailable))
                        {
                            clientObject.Context.Load(clientObject, expression);
                            dirty = true;
                        }
                    }
                    else
                    {
                        throw new Exception("Only 'Include' and 'IncludeWithDefaultProperties' methods are supported.");
                    }
                }
                else if (!clientObject.IsPropertyAvailable(expression) && !clientObject.IsObjectPropertyInstantiated(expression))
                {
                    clientObject.Context.Load(clientObject, expression);
                    dirty = true;
                }
                else if (clientObject.IsObjectPropertyInstantiated(expression))
                {
                    dirty = dirty || EnsureCollectionLoaded(clientObject, expression);
                }
            }

            if (dirty)
            {
                await clientObject.Context.ExecuteQueryRetryAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Converts generic <![CDATA[ Expression<Func<TInput, TOutput>> ]]> to Expression with object return type - <![CDATA[ Expression<Func<TInput, object>> ]]>
        /// </summary>
        /// <typeparam name="TInput">Input type</typeparam>
        /// <typeparam name="TOutput">Returns type</typeparam>
        /// <param name="expression"><see cref="Expression" /> to convert </param>
        /// <returns>New Expression where return type is object and not generic</returns>
        public static Expression<Func<TInput, object>> ToUntypedStaticMethodCallExpression<TInput, TOutput>(this Expression<Func<TInput, TOutput>> expression)
        {
            var body = (MethodCallExpression)expression.Body;
            var clientObjectProperty = (MemberExpression)(body.Arguments[0]);
            var newArrayExpression = body.Arguments[1] as NewArrayExpression;
            var param = Expression.Parameter(typeof(TInput));
            var propertyToCall = (Expression.Property(param, clientObjectProperty.Member.Name));


            return Expression.Lambda<Func<TInput, object>>(Expression.Call(null, body.Method, propertyToCall, newArrayExpression), param);
        }

        /// <summary>
        /// Converts generic <![CDATA[ Expression<Func<TInput, TOutput>> ]]> to Expression with object return type - <![CDATA[ Expression<Func<TInput, object>> ]]>
        /// </summary>
        /// <typeparam name="TInput">Input type</typeparam>
        /// <typeparam name="TOutput">Returns type</typeparam>
        /// <param name="expression"><see cref="Expression" /> to convert </param>
        /// <returns>New Expression where return type is object and not generic</returns>
        public static Expression<Func<TInput, object>> ToUntypedPropertyExpression<TInput, TOutput>(this Expression<Func<TInput, TOutput>> expression)
        {

            var body = expression.Body as MemberExpression ?? ((UnaryExpression)expression.Body).Operand as MemberExpression;

            var memberName = body.Member.Name;

            var param = Expression.Parameter(typeof(TInput));
            var field = Expression.Property(param, memberName);

            return Expression.Lambda<Func<TInput, object>>(
                Expression.Convert(field, typeof(object)),
                param);
        }

        private static bool EnsureCollectionLoaded<T>(T clientObject, Expression<Func<T, object>> propertySelector) where T : ClientObject
        {
            var body = propertySelector.Body as MemberExpression ?? ((UnaryExpression)propertySelector.Body).Operand as MemberExpression;
            var propertyInfo = (PropertyInfo)body.Member;
            var propertyValue = propertyInfo.GetValue(clientObject);

            if (propertyValue is ClientObjectCollection)
            {
                var clientCollection = propertyValue as ClientObjectCollection;
                if (!clientCollection.AreItemsAvailable)
                {
                    clientObject.Context.Load(clientObject, propertySelector);
                    return true;
                }
            }
            return false;
        }

        internal static void ClearObjectData(this ClientObject clientObject)
        {
            var info_ClientObject_ObjectData = typeof(ClientObject)
                .GetProperty("ObjectData", BindingFlags.NonPublic | BindingFlags.Instance);

            var objectData = (ClientObjectData)info_ClientObject_ObjectData.GetValue(clientObject, Array.Empty<object>());
            objectData.MethodReturnObjects.Clear();
        }
    }
}
