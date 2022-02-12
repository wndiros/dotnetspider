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
		/// Find results by regular expression
		/// </summary>
		/// <param name="pattern">Regular Expression</param>
		/// <param name="options"></param>
		/// <param name="group">Group</param>
		/// <returns>Query interface</returns>
		ISelectable Regex(string pattern, RegexOptions options = RegexOptions.None, string replacement = "$0");

		/// <summary>
		/// Get the text result of the current query, if there are multiple query results, return the value of the first result
		/// </summary>
		/// <returns>The text result of the query</returns>
		string Value { get; }

		/// <summary>
		/// Find Results by Queryer
		/// </summary>
		/// <param name="selector">Queryer</param>
		/// <returns>Query interface</returns>
		ISelectable Select(ISelector selector);

		/// <summary>
		/// Find Results by Queryer
		/// </summary>
		/// <param name="selector">Queryer</param>
		/// <returns>Query interface</returns>
		IEnumerable<ISelectable> SelectList(ISelector selector);
    }
}
