using Microsoft.AspNetCore.Mvc;
using Tgs.Calculate;
using Tgs.Calculate.Protos;
using Tgs.CalculateService.ViewModels;
using Valhalla.MessageQueue;

namespace Tgs.CalculateService.Controllers;

[Route("[controller]")]
[ApiController]
public class ExchangeController : ControllerBase
{
	[HttpGet("{calculateId:Guid}", Name = "GetBalance")]
	public async Task<ExchangeResultViewModel> GetBalanceAsync(
		[FromServices] IMessageSender messageSender,
		Guid calculateId)
	{
		var request = new GetBalanceRequest
		{
			CalculateId = calculateId
		};

		var response = await messageSender.RequestAsync<GetBalanceRequest, ExchangeResponse>(
			Subjects.GetBalanceSubject,
			request,
			HttpContext.RequestAborted).ConfigureAwait(false);

		return new ExchangeResultViewModel
		{
			Id = response.SnapshotId,
			PostBalance = response.Balance
		};
	}

	[HttpPost("{calculateId:Guid}", Name = "CreateCalculate")]
	public async Task<ExchangeResultViewModel> CreateAsync(
		[FromServices] IMessageSender messageSender,
		Guid calculateId)
	{
		var request = new CreateRequest
		{
			CalculateId = calculateId
		};

		var response = await messageSender.RequestAsync<CreateRequest, ExchangeResponse>(
			Subjects.CreateSubject,
			request,
			HttpContext.RequestAborted).ConfigureAwait(false);

		return new ExchangeResultViewModel
		{
			Id = response.SnapshotId,
			PostBalance = response.Balance
		};
	}

	[HttpPut("{calculateId:Guid}", Name = "Exchange")]
	public async Task<ExchangeResultViewModel> ExchangeAsync(
		[FromServices] IMessageSender messageSender,
		Guid calculateId,
		ExchangeViewModel viewModel)
	{
		var request = new ExchangeRequest
		{
			CalculateId = calculateId,
			Change = viewModel.Change
		};

		var response = await messageSender.RequestAsync<ExchangeRequest, ExchangeResponse>(
			Subjects.ExchangeSubject,
			request,
			HttpContext.RequestAborted).ConfigureAwait(false);

		return new ExchangeResultViewModel
		{
			Id = response.SnapshotId,
			PostBalance = response.Balance
		};
	}

	[HttpDelete("{calculateId:Guid}", Name = "DeleteCalculate")]
	public async Task<ExchangeResultViewModel> DeleteCalculateAsync(
		[FromServices] IMessageSender messageSender,
		Guid calculateId)
	{
		var request = new DeleteRequest
		{
			CalculateId = calculateId
		};

		var response = await messageSender.RequestAsync<DeleteRequest, ExchangeResponse>(
			Subjects.DeleteSubject,
			request,
			HttpContext.RequestAborted).ConfigureAwait(false);

		return new ExchangeResultViewModel
		{
			Id = response.SnapshotId,
			PostBalance = response.Balance
		};
	}
}
