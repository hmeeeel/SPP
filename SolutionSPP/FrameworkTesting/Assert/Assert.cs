using System;
using System.Collections.Generic;
using System.Linq;
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


    }
}