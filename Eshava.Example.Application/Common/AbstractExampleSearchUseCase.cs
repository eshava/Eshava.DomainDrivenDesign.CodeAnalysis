using Eshava.Core.Linq.Interfaces;
using Eshava.DomainDrivenDesign.Application.UseCases;
using Eshava.Example.Application.Settings;
using Microsoft.Extensions.Options;

namespace Eshava.Example.Application.Common
{
	internal class AbstractExampleSearchUseCase<TRequest, TDto> : AbstractSearchUseCase<TRequest, TDto>
		where TRequest : class
		where TDto : class
	{
		private readonly IOptions<AppSettings> _appSettings;

		public AbstractExampleSearchUseCase(
			IWhereQueryEngine whereQueryEngine,
			ISortingQueryEngine sortingQueryEngine,
			IOptions<AppSettings> appSettings
		) : base(whereQueryEngine, sortingQueryEngine)
		{
			_appSettings = appSettings;
		}
	}
}