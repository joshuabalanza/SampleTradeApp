using Microsoft.AspNetCore.Mvc;
using TradeOps.Core.Models;
using TradeOps.Core.Services;

namespace TradeOps.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TradesController : ControllerBase
{
    private readonly ITradeRepository _repository;
    private readonly ITradeProcessingQueue _queue;
    private readonly IReconciliationService _reconciliationService;
    private readonly ILogger<TradesController> _logger;

    public TradesController(
        ITradeRepository repository,
        ITradeProcessingQueue queue,
        IReconciliationService reconciliationService,
        ILogger<TradesController> logger)
    {
        _repository = repository;
        _queue = queue;
        _reconciliationService = reconciliationService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Trade), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Trade), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IngestTrade([FromBody] IngestTradeRequest request)
    {
        // 1. Validation
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            return BadRequest(new { error = "IdempotencyKey is required." });
        }

        if (request.Quantity <= 0 || request.Price <= 0)
        {
            return BadRequest(new { error = "Quantity and Price must be strictly positive." });
        }

        // 2. Idempotent Ingestion
        var (trade, alreadyExisted) = await _repository.IngestTradeIdempotentAsync(request);

        if (alreadyExisted)
        {
            _logger.LogInformation("Idempotent replay detected for key {Key}. TradeId: {TradeId}", request.IdempotencyKey, trade.TradeId);
            return Ok(trade); // HTTP 200 OK for replay
        }

        // 3. Enqueue to high-throughput background processing channel
        await _queue.EnqueueAsync(trade.TradeId);

        _logger.LogInformation("New trade {TradeId} ingested successfully.", trade.TradeId);
        return CreatedAtAction(nameof(GetById), new { id = trade.TradeId }, trade);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Trade>>> GetAll()
    {
        var trades = await _repository.GetAllAsync();
        return Ok(trades);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Trade>> GetById(Guid id)
    {
        var trade = await _repository.GetByIdAsync(id);
        if (trade is null) return NotFound();
        return Ok(trade);
    }

    [HttpGet("{id:guid}/audit-logs")]
    public async Task<ActionResult<IEnumerable<AuditLog>>> GetAuditLogs(Guid id)
    {
        var logs = await _repository.GetAuditLogsAsync(id);
        return Ok(logs);
    }

    [HttpPost("{id:guid}/reconcile")]
    public async Task<IActionResult> Reconcile(Guid id, [FromBody] BrokerExecutionReport report)
    {
        var reconciled = await _reconciliationService.ReconcileTradeAsync(id, report);
        var trade = await _repository.GetByIdAsync(id);

        return Ok(new
        {
            Reconciled = reconciled,
            Trade = trade
        });
    }
}
