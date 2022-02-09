using System;
using DotnetSpider.Selector;
using Newtonsoft.Json;

namespace DotnetSpider.DataFlow.Parser
{
	/// <summary>
	/// Definition of Target Link Selector
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class FollowRequestSelector : Attribute
	{
		/// <summary>
		/// Querier type
		/// </summary>
		public SelectorType SelectorType { get; set; } = SelectorType.XPath;

		/// <summary>
		/// query expression
		/// </summary>
		public string[] Expressions { get; set; }

#if !NET451
		/// <summary>
		/// Avoid being serialized
		/// </summary>
		[JsonIgnore]
		public override object TypeId => base.TypeId;
#endif

		/// <summary>
		/// Regular expression matching target link
		/// </summary>
		public string[] Patterns { get; set; } = new string[0];
	}
}
