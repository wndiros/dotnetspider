using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DotnetSpider.Http;
using DotnetSpider.Infrastructure;
using DotnetSpider.Selector;

namespace DotnetSpider.DataFlow
{
	/// <summary>
	/// Data stream processor context
	/// </summary>
	public class DataFlowContext : IDisposable
	{
		private readonly Dictionary<string, dynamic> _properties = new();
		private readonly Dictionary<object, dynamic> _data = new();

		public ISelectable Selectable { get; internal set; }

		public SpiderOptions Options { get; }

		/// <summary>
		/// The result returned by the downloader
		/// </summary>
		public Response Response { get; }

		/// <summary>
		/// The content returned by the message queue
		/// </summary>
		public byte[] MessageBytes { get; internal set; }

		/// <summary>
		/// download request
		/// </summary>
		public Request Request { get; }

		/// <summary>
		/// Resolved target link
		/// </summary>
		internal List<Request> FollowRequests { get; }

		public IServiceProvider ServiceProvider { get; }

		/// <summary>
		/// Constructor 
		/// </summary>
		/// <param name="request"></param>
		/// <param name="response">下载器返回的结果</param>
		/// <param name="options"></param>
		/// <param name="serviceProvider"></param>
		public DataFlowContext(IServiceProvider serviceProvider,
			SpiderOptions options,
			Request request,
			Response response
		)
		{
			Request = request;
			Response = response;
			Options = options;
			ServiceProvider = serviceProvider;
			FollowRequests = new List<Request>();
		}

		public void AddFollowRequests(params Request[] requests)
		{
			AddFollowRequests(requests.AsEnumerable());
		}

		public void AddFollowRequests(IEnumerable<Request> requests)
		{
			if (requests != null)
			{
				FollowRequests.AddRange(requests);
			}
		}

		public void AddFollowRequests(IEnumerable<Uri> uris)
		{
			if (uris == null)
			{
				return;
			}

			AddFollowRequests(uris.Select(CreateNewRequest));
		}

		public Request CreateNewRequest(Uri uri)
		{
			uri.NotNull(nameof(uri));
			var request = Request.Clone();
			request.RequestedTimes = 0;
			request.Depth += 1;
			request.Hash = null;
			request.Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
			request.RequestUri = uri;
			return request;
		}

		/// <summary>
		/// get attribute
		/// </summary>
		/// <param name="key">Key</param>
		public dynamic this[string key]
		{
			get => _properties.ContainsKey(key) ? _properties[key] : null;
			set
			{
				if (_properties.ContainsKey(key))
				{
					_properties[key] = value;
				}

				else
				{
					_properties.Add(key, value);
				}
			}
		}

		/// <summary>
		/// Whether to include attributes
		/// </summary>
		/// <param name="key">Key</param>
		/// <returns></returns>
		public bool Contains(string key)
		{
			return _properties.ContainsKey(key);
		}

		/// <summary>
		/// add properties
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		public void Add(string key, dynamic value)
		{
			_properties.Add(key, value);
		}

		/// <summary>
		/// add data item
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="data">Value</param>
		public void AddData(object name, dynamic data)
		{
			if (_data.ContainsKey(name))
			{
				_data[name] = data;
			}

			else
			{
				_data.Add(name, data);
			}
		}

		/// <summary>
		/// get data item
		/// </summary>
		/// <param name="name">Name</param>
		/// <returns></returns>
		public dynamic GetData(object name)
		{
			return _data.ContainsKey(name) ? _data[name] : null;
		}

		/// <summary>
		/// get all data items
		/// </summary>
		/// <returns></returns>
		public IDictionary<object, dynamic> GetData()
		{
			return _data.ToImmutableDictionary();
		}

		/// <summary>
		/// whether to include data items
		/// </summary>
		public bool IsEmpty => _data.Count == 0;

		/// <summary>
		/// clear data
		/// </summary>
		public void Clear()
		{
			_data.Clear();
		}

		public void Dispose()
		{
			_properties.Clear();
			_data.Clear();
			MessageBytes = null;

			ObjectUtilities.DisposeSafely(Request);
			ObjectUtilities.DisposeSafely(Response);
		}
	}
}
