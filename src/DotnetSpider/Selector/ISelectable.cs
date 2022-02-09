using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DotnetSpider.Selector
{
	/// <summary>
	/// query interface
	/// </summary>
	public interface ISelectable
    {
        SelectableType Type { get; }

		/// <summary>
		/// Find results by XPath
		/// </summary>
		/// </param> <param name="xpath">XPath expression</param>
		/// <returns>Query interface</returns>
		ISelectable XPath(string xpath);

		/// <summary>
		/// Find element through CSS selector and get the value of attribute
		/// </summary>
		/// <param name = "css" > Css selector</param
		/// <param name="attr">The attribute of the element queried</param>
		/// <returns>Query interface</returns>
		ISelectable Css(string css, string attr = null);

		/// <summary>
		/// find all links
		/// </summary>
		/// <returns>Query interface</returns>
		IEnumerable<string> Links();

		/// <summary>
		/// Find results by JsonPath
		/// </summary>
		/// <param name="jsonPath">JsonPath expression</param>
		/// <returns>Query interface</returns>
		ISelectable JsonPath(string jsonPath);

        IEnumerable<ISelectable> Nodes();

        /// <summary>
        /// 通过正则表达式查找结果
        /// </summary>
        /// <param name="pattern">正则表达式</param>
        /// <param name="options"></param>
        /// <param name="group">分组</param>
        /// <returns>查询接口</returns>
        ISelectable Regex(string pattern, RegexOptions options = RegexOptions.None, string replacement = "$0");

        /// <summary>
        /// 获得当前查询器的文本结果, 如果查询结果为多个, 则返回第一个结果的值
        /// </summary>
        /// <returns>查询到的文本结果</returns>
        string Value { get; }

        /// <summary>
        /// 通过查询器查找结果
        /// </summary>
        /// <param name="selector">查询器</param>
        /// <returns>查询接口</returns>
        ISelectable Select(ISelector selector);

        /// <summary>
        /// 通过查询器查找结果
        /// </summary>
        /// <param name="selector">查询器</param>
        /// <returns>查询接口</returns>
        IEnumerable<ISelectable> SelectList(ISelector selector);
    }
}
