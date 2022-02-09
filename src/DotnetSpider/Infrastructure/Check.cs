using System;

namespace DotnetSpider.Infrastructure
{
    /// <summary>
    /// 参数合法性检查类
    /// </summary>
    public static class Check
    {
        /// <summary>
        /// 检查参数不能为空引用，否则抛出<see cref="ArgumentNullException"/>异常。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="paramName">参数名称</param>
        public static void NotNull<T>(this T value, string paramName)
        {
            Require<ArgumentException>(value != null, $"参数 {paramName} 不能为空引用");
        }

        public static void NotNullOrDefault<T>(this T value, string paramName)
        {
            if (value == null)
            {
                throw new ArgumentException($"参数 {paramName} 不能为空引用");
            }

            var defaultValue = default(T);
            Require<ArgumentException>(!value.Equals(defaultValue), $"参数 {paramName} 不能为默认值 {defaultValue}");
        }

        /// <summary>
        /// 检查参数不能为空引用，否则抛出<see cref="ArgumentNullException"/>异常。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="paramName">参数名称</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void NotNullOrWhiteSpace(this string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"参数 {paramName} 不能为空或空字符串");
            }
        }

		/// <summary>		
		/// Verify that the assertion <paramref name="assertion"/> of the specified value is true, if not, throw an exception of the specified type <typeparamref name="TException"/> with the specified message <paramref name="message"/>
		/// </summary>
		/// <typeparam name="TException">异常类型</typeparam>
		/// <param name="assertion">要验证的断言。</param>
		/// <param name="message">异常消息。</param>
		private static void Require<TException>(bool assertion, string message)
            where TException : Exception
        {
            if (assertion)
            {
                return;
            }

            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            var exception = (TException) Activator.CreateInstance(typeof(TException), message);
            throw exception;
        }
    }
}
