using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FrameworkTesting.Assert
{
    public static class Assert
    {
        public static void AreEqual<T>(T? expected, T? actual, string? message = null)
        {
            if (!Equals(expected, actual))
            {
                var msg = message ?? $"Ожидалось: <{expected}>, но получено: <{actual}>";
                throw new AssertException(msg, nameof(AreEqual), expected, actual);
            }
        }

        public static void AreNEqual<T>(T? notExpected, T? actual, string? message = null)
        {
            if (Equals(notExpected, actual))
            {
                var msg = message ?? $"Значения должны быть разными, но оба равны: <{actual}>";
                throw new AssertException(msg, nameof(AreNEqual), notExpected, actual);
            }
        }


        public static void IsTrue(bool condition, string? message = null)
        {
            if (!condition)
            {
                var msg = message ?? "IsTrue - должно быть true, но - false";
                throw new AssertException(msg, nameof(IsTrue));
            }
        }


        public static void IsFalse(bool condition, string? message = null)
        {
            if (condition)
            {
                var msg = message ?? "IsFalse - должно быть false, но - true";
                throw new AssertException(msg, nameof(IsFalse));
            }
        }

        public static void IsNull(object? obj, string? message = null)
        {
            if (obj is not null)
            {
                var msg = message ?? $"Ожидался null, но получен объект типа {obj.GetType().Name}";
                throw new AssertException(msg, nameof(IsNull));
            }
        }

        public static void IsNotNull(object? obj, string? message = null)
        {
            if (obj is null)
            {
                var msg = message ?? "Ожидался не-null объект, но получен null";
                throw new AssertException(msg, nameof(IsNotNull));
            }
        }


        public static TException Throws<TException>(Action action, string? message = null)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex; 
            }
            catch (Exception ex)
            {
                var msg = message ??
                    $"Ожидалось исключение {typeof(TException).Name}, но выброшено {ex.GetType().Name}: {ex.Message}";
                throw new AssertException(msg, nameof(Throws));
            }

            var failMsg = message ?? $"Ожидалось исключение {typeof(TException).Name}, но оно не было выброшено";
            throw new AssertException(failMsg, nameof(Throws));
        }


        public static async Task<TException> ThrowsAsync<TException>(Func<Task> action, string? message = null)
            where TException : Exception
        {
            try
            {
                await action();
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                var msg = message ??
                    $"Ожидалось исключение {typeof(TException).Name}, но выброшено {ex.GetType().Name}: {ex.Message}";
                throw new AssertException(msg, nameof(ThrowsAsync));
            }

            var failMsg = message ?? $"Ожидалось исключение {typeof(TException).Name}, но оно не было выброшено";
            throw new AssertException(failMsg, nameof(ThrowsAsync));
        }

        public static void Contains<T>(IEnumerable<T> collection, T item, string? message = null)
        {
            if (!collection.Contains(item))
            {
                var msg = message ?? $"Нет элемента: <{item}>";
                throw new AssertException(msg, nameof(Contains));
            }
        }

        public static void DoesNotContain<T>(IEnumerable<T> collection, T item, string? message = null)
        {
            if (collection.Contains(item))
            {
                var msg = message ?? $"Нет элемента: <{item}>";
                throw new AssertException(msg, nameof(DoesNotContain));
            }
        }

        public static void GreaterThan<T>(T actual, T threshold, string? message = null)
            where T : IComparable<T>
        {
            if (actual.CompareTo(threshold) <= 0)
            {
                var msg = message ?? $"Ожидалось: <{actual}> > <{threshold}>, но это фигня";
                throw new AssertException(msg, nameof(GreaterThan), threshold, actual);
            }
        }


        public static void LessThan<T>(T actual, T threshold, string? message = null)
            where T : IComparable<T>
        {
            if (actual.CompareTo(threshold) >= 0)
            {
                var msg = message ?? $"Ожидалось: <{actual}> < <{threshold}>, но это фигня";
                throw new AssertException(msg, nameof(LessThan), threshold, actual);
            }
        }

        public static void IsInstanceOf<T>(object? obj, string? message = null)
        {
            if (obj is not T)
            {
                var actualType = obj?.GetType().Name ?? "null";
                var msg = message ?? $"Ожидался тип {typeof(T).Name}, но получен {actualType}";
                throw new AssertException(msg, nameof(IsInstanceOf));
            }
        }

        public static void StringContains(string actual, string substring, string? message = null)
        {
            if (!actual.Contains(substring, StringComparison.OrdinalIgnoreCase))
            {
                var msg = message ?? $"Строка \"{actual}\" не содержит подстроку \"{substring}\"";
                throw new AssertException(msg, nameof(StringContains));
            }
        }

        public static void HasCount<T>(IEnumerable<T> collection, int expectedCount, string? message = null)
        {
            var actual = collection.Count();
            if (actual != expectedCount)
            {
                var msg = message ?? $"Ожидалось {expectedCount} элементов, но найдено {actual}";
                throw new AssertException(msg, nameof(HasCount), expectedCount, actual);
            }
        }

        public static void AreEqualWithDelta(double expected, double actual, double delta, string? message = null)
        {
            if (Math.Abs(expected - actual) > delta)
            {
                var msg = message ?? $"Ожидалось: {expected} ± {delta}, но получено: {actual}";
                throw new AssertException(msg, nameof(AreEqualWithDelta), expected, actual);
            }
        }
        public static void That(Expression<Func<bool>> expression, string? message = null)
            {
                if (expression == null) throw new ArgumentNullException(nameof(expression));

                var compiled = expression.Compile();
                bool result = compiled();

                if (!result)
                {
                    var analysis = AnalyzeExpression(expression.Body);
                    
                    var errorMessage = message != null 
                        ? $"{message}\n{analysis}" 
                        : analysis;
                    
                    throw new AssertException(errorMessage, nameof(That));
                }
            }

            private static string AnalyzeExpression(Expression expr)
            {
                var sb = new StringBuilder();
                sb.AppendLine("ДЕТАЛЬНЫЙ АНАЛИЗ:");
                sb.AppendLine($"Выражение: {expr}");
                sb.AppendLine();
                
                AnalyzeNode(expr, sb, indent: 0);
                
                return sb.ToString();
            }

            private static void AnalyzeNode(Expression node, StringBuilder sb, int indent)
            {
                string indentation = new string(' ', indent * 2);
                
                switch (node.NodeType)
                {
                    case ExpressionType.Equal:
                    case ExpressionType.NotEqual:
                    case ExpressionType.LessThan:
                    case ExpressionType.LessThanOrEqual:
                    case ExpressionType.GreaterThan:
                    case ExpressionType.GreaterThanOrEqual:
                    case ExpressionType.AndAlso:
                    case ExpressionType.OrElse:
                    case ExpressionType.Add:
                    case ExpressionType.Subtract:
                    case ExpressionType.Multiply:
                    case ExpressionType.Divide:
                        AnalyzeBinaryExpression((BinaryExpression)node, sb, indent, indentation);
                        break;

                    case ExpressionType.Not:
                        AnalyzeUnaryExpression((UnaryExpression)node, sb, indent, indentation);
                        break;

                    case ExpressionType.MemberAccess:
                        AnalyzeMemberExpression((MemberExpression)node, sb, indentation);
                        break;

                    case ExpressionType.Constant:
                        AnalyzeConstantExpression((ConstantExpression)node, sb, indentation);
                        break;

                    case ExpressionType.Call:
                        AnalyzeMethodCallExpression((MethodCallExpression)node, sb, indent, indentation);
                        break;

                    default:
                        sb.AppendLine($"{indentation}[{node.NodeType}] {node}");
                        break;
                }
            }

            private static void AnalyzeBinaryExpression(BinaryExpression binary, StringBuilder sb, int indent, string indentation)
            {
                string operatorSymbol = binary.NodeType switch
                {
                    ExpressionType.Equal => "==",
                    ExpressionType.NotEqual => "!=",
                    ExpressionType.LessThan => "<",
                    ExpressionType.LessThanOrEqual => "<=",
                    ExpressionType.GreaterThan => ">",
                    ExpressionType.GreaterThanOrEqual => ">=",
                    ExpressionType.AndAlso => "&&",
                    ExpressionType.OrElse => "||",
                    ExpressionType.Add => "+",
                    ExpressionType.Subtract => "-",
                    ExpressionType.Multiply => "*",
                    ExpressionType.Divide => "/",
                    _ => binary.NodeType.ToString()
                };

                sb.AppendLine($"{indentation}БИНАРНАЯ ОПЕРАЦИЯ: {operatorSymbol}");
                
                sb.AppendLine($"{indentation}|- ЛЕВЫЙ:");
                AnalyzeNode(binary.Left, sb, indent + 1);
                object? leftValue = EvaluateExpression(binary.Left);
                sb.AppendLine($"{indentation}|  *- Значение: {FormatValue(leftValue)}");
                
                sb.AppendLine($"{indentation}*- ПРАВЫЙ:");
                AnalyzeNode(binary.Right, sb, indent + 1);
                object? rightValue = EvaluateExpression(binary.Right);
                sb.AppendLine($"{indentation}   *- Значение: {FormatValue(rightValue)}");
                
                object? result = EvaluateExpression(binary);
                sb.AppendLine($"{indentation}РЕЗУЛЬТАТ: {FormatValue(leftValue)} {operatorSymbol} {FormatValue(rightValue)} = {FormatValue(result)}");
            }


            private static void AnalyzeUnaryExpression(UnaryExpression unary, StringBuilder sb, int indent, string indentation)
            {
                string operatorSymbol = unary.NodeType == ExpressionType.Not ? "!" : unary.NodeType.ToString();
                
                sb.AppendLine($"{indentation}УНАРНАЯ ОПЕРАЦИЯ: {operatorSymbol}");
                sb.AppendLine($"{indentation}└─ ОПЕРАНД:");
                AnalyzeNode(unary.Operand, sb, indent + 1);
                object? value = EvaluateExpression(unary.Operand);
                sb.AppendLine($"{indentation}   └─ Значение: {FormatValue(value)}");
            }

            private static void AnalyzeMemberExpression(MemberExpression member, StringBuilder sb, string indentation)
            {
                sb.AppendLine($"{indentation}ЧЛЕН: {member.Member.Name}");
                
                if (member.Expression != null)
                {
                    object? instance = EvaluateExpression(member.Expression);
                    sb.AppendLine($"{indentation}*- Объект: {instance?.GetType().Name ?? "null"}");
                }
            }


            private static void AnalyzeConstantExpression(ConstantExpression constant, StringBuilder sb, string indentation)
            {
                sb.AppendLine($"{indentation}КОНСТАНТА: {FormatValue(constant.Value)}");
            }


            private static void AnalyzeMethodCallExpression(MethodCallExpression call, StringBuilder sb, int indent, string indentation)
            {
                sb.AppendLine($"{indentation}ВЫЗОВ МЕТОДА: {call.Method.Name}");
                
                if (call.Object != null)
                {
                    sb.AppendLine($"{indentation}|- НА ОБЪЕКТЕ:");
                    AnalyzeNode(call.Object, sb, indent + 1);
                }
                
                if (call.Arguments.Count > 0)
                {
                    sb.AppendLine($"{indentation}*- АРГУМЕНТЫ:");
                    for (int i = 0; i < call.Arguments.Count; i++)
                    {
                        sb.AppendLine($"{indentation}   [{i}]:");
                        AnalyzeNode(call.Arguments[i], sb, indent + 2);
                        object? argValue = EvaluateExpression(call.Arguments[i]);
                        sb.AppendLine($"{indentation}      *-Значение: {FormatValue(argValue)}");
                    }
                }
            }


            private static object? EvaluateExpression(Expression expr)
            {
                var lambda = Expression.Lambda<Func<object>>( Expression.Convert(expr, typeof(object)));
                return lambda.Compile()();
            }


            private static string FormatValue(object? value)
            {
                if (value == null) return "null";
                
                if (value is string s) return $"\"{s}\"";
                if (value is bool b) return b.ToString().ToUpper();
                if (value is decimal || value is double || value is float)
                    return $"{value} ({value.GetType().Name})";
                
                return $"{value} (тип: {value.GetType().Name})";
            }
    }
}