using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Unisave.Facets
{
    /// <summary>
    /// Can evaluate LINQ expressions.
    /// 
    /// Using the Expression.Lambda(...).Compile().DynamicInvoke()
    /// is too complicated and breaks under IL2CPP for older Unity versions.
    /// So this exists instead.
    /// </summary>
    public static class LinqExpressionInterpreter
    {
        /// <summary>
        /// Call this to evaluate a LINQ expression in the facet call API
        /// </summary>
        /// <param name="expression">Given LINQ expression</param>
        /// <returns>The expression value</returns>
        public static object Interpret(Expression expression)
        {
            return Visit(expression);
        }
        
        /// <summary>
        /// Thrown when the interpreting fails
        /// </summary>
        public class InterpretingException : Exception
        {
            public InterpretingException()
            { }

            public InterpretingException(string message) : base(message)
            { }

            public InterpretingException(string message, Exception inner)
                : base(message, inner) { }
        }
        
        
        //////////////
        // Visiting //
        //////////////
        
        private static object Visit(Expression node)
        {
            switch (node)
            {
                case null:
                    return null;
                
                case ConstantExpression e:
                    return e.Value;
                
                case UnaryExpression e:
                    return VisitUnary(e);
                
                case BinaryExpression e:
                    return VisitBinary(e);
                
                case ConditionalExpression e:
                    return VisitConditional(e);
                
                case MemberExpression e:
                    return VisitMemberAccess(e);
                
                case MethodCallExpression e:
                    return VisitCall(e);
                
                case NewExpression e:
                    return VisitNewExpression(e);
                
                case NewArrayExpression e:
                    return VisitNewArrayExpression(e);
            }

            throw new InterpretingException(
                $"The LINQ expression node {node} [{node.NodeType}] is not supported."
            );
        }

        private static object VisitUnary(UnaryExpression node)
        {
            object operand = Visit(node.Operand);
            
            
            // === Interpret method-defined operators ===
            
            if (node.Method != null)
                return node.Method.Invoke(null, new[] {operand});
            
            
            // === Interpret upcasting and (un)boxing conversions ===

            if (node.NodeType == ExpressionType.Convert)
            {
                // up-casting (e.g. string to object)
                if (node.Type.IsAssignableFrom(node.Operand.Type))
                    return operand;
                
                // boxing value types to object (e.g. int to object)
                if (node.Type == typeof(object))
                    return operand;
                
                // down-casting from object
                // (do nothing and it will happen later, since I return object)
                if (node.Operand.Type.IsAssignableFrom(node.Type))
                    return operand;
                
                // unboxing from object
                // (do nothing and it will happen later, since I return object)
                if (node.Operand.Type == typeof(object))
                    return operand;
            }
            
            
            // === Interpret primitive value operators ===
            
            Type valueType = node.Operand.Type;
            if (valueType != node.Type)
                goto fail;
            
            switch (node.NodeType)
            {
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    return Convert.ChangeType(operand, node.Type);
                
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    if (valueType == typeof(double)) return -(double) operand;
                    if (valueType == typeof(float)) return -(float) operand;
                    if (valueType == typeof(long)) return -(long) operand;
                    if (valueType == typeof(int)) return -(int) operand;
                    break;
                
                case ExpressionType.Not:
                    if (valueType == typeof(bool)) return !(bool) operand;
                    break;
            }
            
            
            // === Fail ===
            
            fail: throw new InterpretingException(
                $"Cannot interpret unary node because it's not implemented: " +
                $"[{node.NodeType}@{node.Type}] [{node.Operand.Type}]"
            );
        }

        private static object VisitBinary(BinaryExpression node)
        {
            // lambdas instead of variables to defer the execution until needed
            // (so that &&, ||, ?? and other such operators work correctly)
            Func<object> left = () => Visit(node.Left);
            Func<object> right = () => Visit(node.Right);
            
            
            // === Interpret method-defined operators ===
            
            if (node.Method != null)
                return node.Method.Invoke(null, new[] {left(), right()});
            
            
            // === Interpret null-coalescing operator "??" ===

            if (node.NodeType == ExpressionType.Coalesce)
                return left() ?? right();
            
            
            // === Interpret primitive value operators ===
            
            // the primitive operation value type
            // the output node.Type might be different (say bool for comparisons)
            Type valueType = node.Left.Type;
            if (valueType != node.Right.Type)
                goto fail;
            
            switch (node.NodeType)
            {
                // NUMERIC //
                
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    if (valueType == typeof(double)) return (double) left() + (double) right();
                    if (valueType == typeof(float)) return (float) left() + (float) right();
                    if (valueType == typeof(long)) return (long) left() + (long) right();
                    if (valueType == typeof(int)) return (int) left() + (int) right();
                    break;
                
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    if (valueType == typeof(double)) return (double) left() - (double) right();
                    if (valueType == typeof(float)) return (float) left() - (float) right();
                    if (valueType == typeof(long)) return (long) left() - (long) right();
                    if (valueType == typeof(int)) return (int) left() - (int) right();
                    break;
                
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    if (valueType == typeof(double)) return (double) left() * (double) right();
                    if (valueType == typeof(float)) return (float) left() * (float) right();
                    if (valueType == typeof(long)) return (long) left() * (long) right();
                    if (valueType == typeof(int)) return (int) left() * (int) right();
                    break;
                
                case ExpressionType.Divide:
                    if (valueType == typeof(double)) return (double) left() / (double) right();
                    if (valueType == typeof(float)) return (float) left() / (float) right();
                    if (valueType == typeof(long)) return (long) left() / (long) right();
                    if (valueType == typeof(int)) return (int) left() / (int) right();
                    break;
                
                case ExpressionType.Modulo:
                    if (valueType == typeof(double)) return (double) left() % (double) right();
                    if (valueType == typeof(float)) return (float) left() % (float) right();
                    if (valueType == typeof(long)) return (long) left() % (long) right();
                    if (valueType == typeof(int)) return (int) left() % (int) right();
                    break;
                
                // EQUALITY //
                
                case ExpressionType.Equal:
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (valueType == typeof(double)) return (double) left() == (double) right();
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (valueType == typeof(float)) return (float) left() == (float) right();
                    if (valueType == typeof(long)) return (long) left() == (long) right();
                    if (valueType == typeof(int)) return (int) left() == (int) right();
                    if (valueType == typeof(bool)) return (bool) left() == (bool) right();
                    break;
                
                case ExpressionType.NotEqual:
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (valueType == typeof(double)) return (double) left() != (double) right();
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (valueType == typeof(float)) return (float) left() != (float) right();
                    if (valueType == typeof(long)) return (long) left() != (long) right();
                    if (valueType == typeof(int)) return (int) left() != (int) right();
                    if (valueType == typeof(bool)) return (bool) left() != (bool) right();
                    break;
                
                // COMPARISON //
                
                case ExpressionType.GreaterThan:
                    if (valueType == typeof(double)) return (double) left() > (double) right();
                    if (valueType == typeof(float)) return (float) left() > (float) right();
                    if (valueType == typeof(long)) return (long) left() > (long) right();
                    if (valueType == typeof(int)) return (int) left() > (int) right();
                    break;
                
                case ExpressionType.GreaterThanOrEqual:
                    if (valueType == typeof(double)) return (double) left() >= (double) right();
                    if (valueType == typeof(float)) return (float) left() >= (float) right();
                    if (valueType == typeof(long)) return (long) left() >= (long) right();
                    if (valueType == typeof(int)) return (int) left() >= (int) right();
                    break;
                
                case ExpressionType.LessThan:
                    if (valueType == typeof(double)) return (double) left() < (double) right();
                    if (valueType == typeof(float)) return (float) left() < (float) right();
                    if (valueType == typeof(long)) return (long) left() < (long) right();
                    if (valueType == typeof(int)) return (int) left() < (int) right();
                    break;
                
                case ExpressionType.LessThanOrEqual:
                    if (valueType == typeof(double)) return (double) left() <= (double) right();
                    if (valueType == typeof(float)) return (float) left() <= (float) right();
                    if (valueType == typeof(long)) return (long) left() <= (long) right();
                    if (valueType == typeof(int)) return (int) left() <= (int) right();
                    break;
                
                // BITWISE //
                
                case ExpressionType.And:
                    if (valueType == typeof(long)) return (long) left() & (long) right();
                    if (valueType == typeof(int)) return (int) left() & (int) right();
                    if (valueType == typeof(bool)) return (bool) left() & (bool) right();
                    break;
                
                case ExpressionType.Or:
                    if (valueType == typeof(long)) return (long) left() | (long) right();
                    if (valueType == typeof(int)) return (int) left() | (int) right();
                    if (valueType == typeof(bool)) return (bool) left() | (bool) right();
                    break;
                
                case ExpressionType.ExclusiveOr:
                    if (valueType == typeof(long)) return (long) left() ^ (long) right();
                    if (valueType == typeof(int)) return (int) left() ^ (int) right();
                    if (valueType == typeof(bool)) return (bool) left() ^ (bool) right();
                    break;
                
                // BOOLEAN //
                
                case ExpressionType.AndAlso:
                    if (valueType == typeof(bool)) return (bool) left() && (bool) right();
                    break;
                
                case ExpressionType.OrElse:
                    if (valueType == typeof(bool)) return (bool) left() || (bool) right();
                    break;
            }
            
            
            // === Fail ===

            fail: throw new InterpretingException(
                $"Cannot interpret binary node because it's not implemented: " +
                $"[{node.Left.Type}] [{node.NodeType}@{node.Type}] [{node.Right.Type}]"
            );
        }

        private static object VisitConditional(ConditionalExpression node)
        {
            bool condition = (bool) Visit(node.Test);

            if (condition)
                return Visit(node.IfTrue);
            else
                return Visit(node.IfFalse);
        }
        
        private static object VisitMemberAccess(MemberExpression node)
        {
            object instance = Visit(node.Expression);
    
            switch (node.Member)
            {
                case FieldInfo fi:
                    if (instance == null && !fi.IsStatic)
                        throw new NullReferenceException(
                            $"Member access evaluated to null: {node}."
                        );
                    return fi.GetValue(instance);
        
                case PropertyInfo pi:
                    if (instance == null && !pi.GetMethod.IsStatic)
                        throw new NullReferenceException(
                            $"Member access evaluated to null: {node}."
                        );
                    return pi.GetValue(instance);
            }
    
            throw new InterpretingException(
                $"Unsupported member access node {node} [{node.Member.GetType()}]."
            );
        }

        private static object VisitCall(MethodCallExpression node)
        {
            object instance = Visit(node.Object);
            object[] args = node.Arguments
                .Select(Visit)
                .ToArray();

            return node.Method.Invoke(instance, args);
        }

        private static object VisitNewExpression(NewExpression node)
        {
            object[] args = node.Arguments.Select(Visit).ToArray();
            return node.Constructor.Invoke(args);
        }

        private static object VisitNewArrayExpression(NewArrayExpression node)
        {
            Array array = Array.CreateInstance(
                node.Type.GetElementType() ?? throw new InterpretingException(
                    "Element type of new array expression is null."
                ),
                node.Expressions.Count
            );
            
            for (int i = 0; i < node.Expressions.Count; i++)
            {
                object item = Visit(node.Expressions[i]);
                array.SetValue(item, i);
            }

            return array;
        }
    }
}