using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotnetSpider.Http;
using DotnetSpider.Infrastructure;
using DotnetSpider.Scheduler.Component;

namespace DotnetSpider.Scheduler
{
	/// <summary>
	/// Memory-based breadth-first scheduling (without deduplicating URLs)
	/// </summary>
	public class QueueBfsScheduler : SchedulerBase
	{
		private readonly List<Request> _requests =
			new();

		/// <summary>
		/// Constructer method
		/// </summary>
		public QueueBfsScheduler(IRequestHasher requestHasher) : base(new FakeDuplicateRemover(), requestHasher)
		{
		}

		public override void Dispose()
		{
			_requests.Clear();
			base.Dispose();
		}

		/// <summary>
		/// add to the queue if the request is not repeated
		/// </summary>
		/// <param name="request">ask</param>
		protected override Task PushWhenNoDuplicate(Request request)
		{
			if (request == null)
			{
				return Task.CompletedTask;
			}

			_requests.Add(request);
			return Task.CompletedTask;
		}

		/// <summary>
		/// Take the specified number of requests from the specified crawler from the queue
		/// </summary>
		/// <param name="count">Number of queues</param>
		/// <returns>request</returns>
		protected override Task<IEnumerable<Request>> ImplDequeueAsync(int count = 1)
		{
			var requests = _requests.Take(count).ToArray();
			if (requests.Length > 0)
			{
				_requests.RemoveRange(0, count);
			}

			return Task.FromResult(requests.Select(x => x.Clone()));
		}
	}
}
