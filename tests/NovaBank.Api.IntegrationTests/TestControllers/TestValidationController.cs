using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NovaBank.Api.Controllers;

namespace NovaBank.Api.IntegrationTests.TestControllers;

public sealed record TestAddress(string City);

public sealed record TestItem(string Name);

public sealed record TestRequest(string FullName, int Id, TestAddress? Address = null, List<TestItem>? Items = null);

public sealed class TestRequestValidator : AbstractValidator<TestRequest>
{
    public TestRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Ad zorunludur.");
        RuleFor(x => x.FullName)
            .MustAsync(async (name, ct) =>
            {
                await Task.Delay(1, ct); // Gerçekten asenkron çalışan bir kural.
                return name != "kayitli";
            })
            .WithMessage("Bu ad kayıtlı.");
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id pozitif olmalı.");
        RuleFor(x => x.Address!.City).NotEmpty().WithMessage("Şehir zorunludur.").When(x => x.Address is not null);
        RuleForEach(x => x.Items).ChildRules(item =>
            item.RuleFor(i => i.Name).NotEmpty().WithMessage("Kalem adı zorunludur."));
    }
}

// İş işleminin çalışıp çalışmadığını testte görmek için.
public sealed class TestWorkCounter
{
    public int Count;
}

// Yalnızca testlerde var; production API'de bu adresler bulunmaz.
[Route("test/validation")]
public sealed class TestValidationController(IValidator<TestRequest> validator, TestWorkCounter counter)
    : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(TestRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        Interlocked.Increment(ref counter.Count);
        return OkResponse(new { request.FullName });
    }
}
