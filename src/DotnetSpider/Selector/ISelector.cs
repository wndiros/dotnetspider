using System.Collections.Generic;

namespace DotnetSpider.Selector
{
	/// <summary>
	/// Inquirer
	/// </summary>
	public interface ISelector
	{
		/// <summary>
		/// Query a single result from text
		/// If there are multiple matching results, only the first one will be returned
		/// </summary>
		/// <param name="text">text to query </param>
		/// <returns>search result</returns>
		ISelectable Select(string text);

		/// <summary>
		/// Query all results from text
		/// </summary>
		/// <param name="text">text to query</param>
		/// <returns>search result</returns>
		IEnumerable<ISelectable> SelectList(string text);
	}
}
