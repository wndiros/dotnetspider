using System;
using System.Reflection;
using DotnetSpider.Selector;

namespace DotnetSpider.DataFlow.Parser
{
	/// <summary>
	/// 属性选择器的定义
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class ValueSelector : Selector
	{
		/// <summary>
		/// Property reflection, used to set parsed values ​​to entity objects
		/// </summary>
		internal PropertyInfo PropertyInfo { get; set; }

		/// <summary>
		/// Whether the value can be empty, if it cannot be empty but the parsed value is empty, the current object is discarded
		/// </summary>
		internal bool NotNull { get; set; }

		/// <summary>
		/// 构造方法
		/// </summary>
		public ValueSelector()
		{
		}

		/// <summary>
		/// 构造方法
		/// </summary>
		/// <param name="type">Selector type</param>
		/// <param name="expression">expression</param>
		public ValueSelector(string expression, SelectorType type = SelectorType.XPath)
			: base(expression, type)
		{
		}

		/// <summary>
		/// 数据格式化
		/// </summary>
		public Formatter[] Formatters { get; set; }
	}
}
