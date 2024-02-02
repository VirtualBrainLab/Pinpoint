using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Unisave.Facades;
using UnityEngine;

namespace Unisave.Facets
{
    /// <summary>
    /// Allows you to call facets
    /// </summary>
    public static class FacetClient
    {
        private static IApplicationLayerFacetCaller Caller
            => new LegacyAdapter(
                ClientFacade.ClientApp.Services.Resolve<FacetCaller>()
            );

        public static UnisaveOperation CallFacet<TFacet>(
            Expression<Action<TFacet>> lambda
        ) where TFacet : Facet
        {
            return CallFacet(null, lambda);
        }
        
        public static UnisaveOperation<TReturn> CallFacet<TFacet, TReturn>(
            Expression<Func<TFacet, TReturn>> lambda
        ) where TFacet : Facet
        {
            return CallFacet(null, lambda);
        }
        
        public static UnisaveOperation CallFacet<TFacet>(
            this MonoBehaviour caller,
            Expression<Action<TFacet>> lambda
        ) where TFacet : Facet
        {
            ArgumentException lambdaException = ParseLambda(
                lambda,
                out MethodInfo method,
                out object[] arguments
            );

            if (lambdaException != null)
                throw lambdaException;

            Task task = Caller.CallFacetMethodAsync(method, arguments);

            return new UnisaveOperation(caller, task);
        }
        
        public static UnisaveOperation<TReturn> CallFacet<TFacet, TReturn>(
            this MonoBehaviour caller,
            Expression<Func<TFacet, TReturn>> lambda
        ) where TFacet : Facet
        {
            ArgumentException lambdaException = ParseLambda(
                lambda,
                out MethodInfo method,
                out object[] arguments
            );

            if (lambdaException != null)
                throw lambdaException;

            Task<TReturn> task = Caller.CallFacetMethodAsync<TReturn>(
                method, arguments
            );

            return new UnisaveOperation<TReturn>(caller, task);
        }

        private static ArgumentException ParseLambda(
            LambdaExpression lambda,
            out MethodInfo method,
            out object[] arguments
        )
        {
            method = null;
            arguments = null;
            
            if (lambda.Parameters.Count != 1)
                return Invalid("It needs to have exactly 1 parameter.");

            ParameterExpression parameter = lambda.Parameters[0];

            if (!typeof(Facet).IsAssignableFrom(parameter.Type))
                return Invalid($"The parameter {parameter.Name} has to " +
                               $"be a {nameof(Facet)}");

            var callExpression = lambda.Body as MethodCallExpression;

            if (callExpression == null)
                return Invalid("The body is not a single facet method call.");

            if (callExpression.Object != parameter)
                return Invalid($"You need to call a method on " +
                               $"the {parameter.Name} parameter.");

            method = callExpression.Method;
            arguments = new object[callExpression.Arguments.Count];

            for (int i = 0; i < arguments.Length; i++)
            {
                // NOTE: Does not work on 2021.3.24 on WebGL.
                // Because it does not AOT compile all necessary generic methods.
                // (even though the link.xml is set up right, this is not because of pruning)
                // Supposedly it should work on 2022.2 and up, but that's too recent.
                //
                //   var argumentLambda = Expression.Lambda(callExpression.Arguments[i]);
                //   var argumentDelegate = argumentLambda.Compile();
                //   arguments[i] = argumentDelegate.DynamicInvoke();
                
                // Instead, let's interpret the expression tree manually
                arguments[i] = LinqExpressionInterpreter.Interpret(
                    callExpression.Arguments[i]
                );
            }
            
            return null;
        }

        private static ArgumentException Invalid(string reason)
        {
            return new ArgumentException(
                $"The {nameof(CallFacet)} argument has to be a lambda " +
                $"expression that calls one facet method. The current lambda " +
                $"is invalid because: " + reason,
                
                // ReSharper disable once NotResolvedInText
                "lambda"
            );
        }
        
        /// <summary>
        /// Connects the new application-layer facet calling API
        /// with the legacy FacetCaller API
        ///
        /// This is a temporary solution as a proper transport-layer API
        /// should be implemented instead.
        /// </summary>
        private class LegacyAdapter : IApplicationLayerFacetCaller
        {
            private readonly FacetCaller facetCaller;
            
            public LegacyAdapter(FacetCaller facetCaller)
            {
                this.facetCaller = facetCaller;
            }

            public Task<TReturn> CallFacetMethodAsync<TReturn>(
                MethodInfo method,
                object[] arguments
            )
            {
                var source = new TaskCompletionSource<TReturn>();

                facetCaller.CallFacetMethod(
                    method.DeclaringType,
                    method.ReturnType,
                    method.Name,
                    arguments
                )
                    .Then((object r) => {
                        source.SetResult(
                            // handles null unboxing for value types
                            (TReturn)(r ?? default(TReturn))
                        );
                    })
                    .Catch(e => {
                        source.SetException(e);
                    });

                return source.Task;
            }
            
            public Task CallFacetMethodAsync(
                MethodInfo method,
                object[] arguments
            )
            {
                var source = new TaskCompletionSource<object>();
                
                facetCaller.CallFacetMethod(
                        method.DeclaringType,
                        method.Name,
                        arguments
                    )
                    .Then(() => {
                        source.SetResult(null);
                    })
                    .Catch(e => {
                        source.SetException(e);
                    });

                return source.Task;
            }
        }
    }
}