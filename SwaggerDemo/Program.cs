using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Swashbuckle anyOf/oneOf Demo",
        Version = "v1",
        Description = "Demonstrates how Swashbuckle handles oneOf, anyOf, and multiple [ProducesResponseType]"
    });

    // Enable oneOf for polymorphic types (inheritance-based)
    options.UseOneOfForPolymorphism();

    // Select which subtypes to include for polymorphism
    options.SelectSubTypesUsing(baseType =>
    {
        if (baseType == typeof(Shape))
            return [typeof(Circle), typeof(Rectangle)];
        if (baseType == typeof(NotificationResult))
            return [typeof(EmailResult), typeof(SmsResult)];
        return [];
    });

    // Register the custom filter that manually injects anyOf
    options.OperationFilter<AnyOfResponseFilter>();
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    // Serve Swagger UI at the root for convenience
    options.RoutePrefix = string.Empty;
});

app.MapControllers();
app.Run();

// =============================================================
// MODELS
// =============================================================

// --- Scenario 1: Polymorphism (oneOf via inheritance) ---

[JsonDerivedType(typeof(Circle), "circle")]
[JsonDerivedType(typeof(Rectangle), "rectangle")]
public abstract class Shape
{
    public string Color { get; set; } = "red";
}

public class Circle : Shape
{
    public double Radius { get; set; }
}

public class Rectangle : Shape
{
    public double Width { get; set; }
    public double Height { get; set; }
}

// --- Scenario 2: Unrelated types (for anyOf demo) ---

public class EmailConfig
{
    public string Email { get; set; } = "";
    public string[]? Cc { get; set; }
}

public class SmsConfig
{
    public string PhoneNumber { get; set; } = "";
    public string? Region { get; set; }
}

[JsonDerivedType(typeof(EmailResult), "email")]
[JsonDerivedType(typeof(SmsResult), "sms")]
public abstract class NotificationResult
{
    public string MessageId { get; set; } = "";
}

public class EmailResult : NotificationResult
{
    public string DeliveredTo { get; set; } = "";
}

public class SmsResult : NotificationResult
{
    public string Carrier { get; set; } = "";
}

// --- Scenario 3: Multiple ProducesResponseType types ---

public class SuccessResponse
{
    public string Message { get; set; } = "OK";
    public int ItemCount { get; set; }
}

public class DetailedResponse
{
    public string Message { get; set; } = "OK";
    public string[] Items { get; set; } = [];
    public DateTime Timestamp { get; set; }
}

public class ErrorResponse
{
    public string Error { get; set; } = "";
    public int Code { get; set; }
}

// =============================================================
// CONTROLLERS
// =============================================================

[ApiController]
[Route("api/[controller]")]
public class ShapesController : ControllerBase
{
    /// <summary>
    /// Scenario 1: oneOf via polymorphism.
    /// Swashbuckle generates a oneOf schema because Shape has subtypes
    /// and UseOneOfForPolymorphism() is enabled.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(Shape), 200)]
    public IActionResult GetShape()
    {
        Shape shape = Random.Shared.Next(2) == 0
            ? new Circle { Color = "blue", Radius = 5 }
            : new Rectangle { Color = "green", Width = 10, Height = 20 };
        return Ok(shape);
    }

    /// <summary>
    /// Also accepts a polymorphic Shape in the request body.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Shape), 201)]
    public IActionResult CreateShape([FromBody] Shape shape)
    {
        return CreatedAtAction(nameof(GetShape), shape);
    }
}

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    /// <summary>
    /// Scenario 2: anyOf via custom IOperationFilter.
    /// The AnyOfResponseFilter manually patches the 200 response schema
    /// to use anyOf with EmailConfig and SmsConfig.
    /// Look at the generated spec to see the difference!
    /// </summary>
    [HttpPost("send")]
    [ProducesResponseType(typeof(NotificationResult), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [AnyOfResponse(200, typeof(EmailResult), typeof(SmsResult))]
    public IActionResult SendNotification([FromBody] EmailConfig config)
    {
        NotificationResult result = new EmailResult
        {
            MessageId = Guid.NewGuid().ToString(),
            DeliveredTo = config.Email
        };
        return Ok(result);
    }

    /// <summary>
    /// Scenario 2b: anyOf on the REQUEST body.
    /// The filter patches the request schema to anyOf EmailConfig | SmsConfig.
    /// </summary>
    [HttpPost("send-any")]
    [ProducesResponseType(typeof(NotificationResult), 200)]
    [AnyOfRequest(typeof(EmailConfig), typeof(SmsConfig))]
    public IActionResult SendAnyNotification([FromBody] object config)
    {
        return Ok(new SmsResult { MessageId = Guid.NewGuid().ToString(), Carrier = "Verizon" });
    }
}

[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    /// <summary>
    /// Scenario 3: Multiple [ProducesResponseType] for the SAME status code (200).
    /// Swashbuckle will only keep the LAST one — observe in the spec!
    /// </summary>
    [HttpGet("multiple-produces")]
    [ProducesResponseType(typeof(SuccessResponse), 200)]   // This one gets LOST
    [ProducesResponseType(typeof(DetailedResponse), 200)]  // Only THIS one appears
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public IActionResult GetWithMultipleProduces([FromQuery] bool detailed = false)
    {
        if (detailed)
            return Ok(new DetailedResponse { Items = ["a", "b"], Timestamp = DateTime.UtcNow });
        return Ok(new SuccessResponse { ItemCount = 42 });
    }

    /// <summary>
    /// Scenario 3b: Multiple [ProducesResponseType] for DIFFERENT status codes.
    /// This works perfectly fine — each status code gets its own schema.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DetailedResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public IActionResult GetItem(int id)
    {
        if (id <= 0)
            return NotFound(new ErrorResponse { Error = "Not found", Code = 404 });
        return Ok(new DetailedResponse { Message = $"Item {id}", Items = ["x"], Timestamp = DateTime.UtcNow });
    }
}

// =============================================================
// CUSTOM ATTRIBUTE + FILTER for anyOf
// =============================================================

[AttributeUsage(AttributeTargets.Method)]
public class AnyOfResponseAttribute : Attribute
{
    public int StatusCode { get; }
    public Type[] Types { get; }
    public AnyOfResponseAttribute(int statusCode, params Type[] types)
    {
        StatusCode = statusCode;
        Types = types;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class AnyOfRequestAttribute : Attribute
{
    public Type[] Types { get; }
    public AnyOfRequestAttribute(params Type[] types) => Types = types;
}

/// <summary>
/// Custom IOperationFilter that injects anyOf schemas where annotated.
/// This is the workaround since Swashbuckle doesn't natively support anyOf.
/// </summary>
public class AnyOfResponseFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Handle anyOf on responses
        var responseAttr = context.MethodInfo
            .GetCustomAttributes(typeof(AnyOfResponseAttribute), false)
            .OfType<AnyOfResponseAttribute>()
            .FirstOrDefault();

        if (responseAttr != null)
        {
            var key = responseAttr.StatusCode.ToString();
            if (operation.Responses.TryGetValue(key, out var response))
            {
                var anyOfSchemas = responseAttr.Types
                    .Select(t => context.SchemaGenerator.GenerateSchema(t, context.SchemaRepository))
                    .ToList();

                var anyOfSchema = new OpenApiSchema { AnyOf = anyOfSchemas };

                if (response.Content.ContainsKey("application/json"))
                    response.Content["application/json"].Schema = anyOfSchema;
            }
        }

        // Handle anyOf on request body
        var requestAttr = context.MethodInfo
            .GetCustomAttributes(typeof(AnyOfRequestAttribute), false)
            .OfType<AnyOfRequestAttribute>()
            .FirstOrDefault();

        if (requestAttr != null && operation.RequestBody?.Content != null)
        {
            var anyOfSchemas = requestAttr.Types
                .Select(t => context.SchemaGenerator.GenerateSchema(t, context.SchemaRepository))
                .ToList();

            var anyOfSchema = new OpenApiSchema { AnyOf = anyOfSchemas };

            if (operation.RequestBody.Content.ContainsKey("application/json"))
                operation.RequestBody.Content["application/json"].Schema = anyOfSchema;
        }
    }
}
