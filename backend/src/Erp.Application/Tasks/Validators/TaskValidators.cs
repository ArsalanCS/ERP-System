using Erp.Application.Tasks.Contracts;
using FluentValidation;

namespace Erp.Application.Tasks.Validators;

internal sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.EstimatedTime).GreaterThanOrEqualTo(0).When(x => x.EstimatedTime.HasValue);
        RuleFor(x => x.DueAt)
            .GreaterThanOrEqualTo(x => x.StartAt!.Value)
            .When(x => x.StartAt.HasValue && x.DueAt.HasValue)
            .WithMessage("Due date must be on or after the start date.");
    }
}

internal sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.CompletionPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.EstimatedTime).GreaterThanOrEqualTo(0).When(x => x.EstimatedTime.HasValue);
        RuleFor(x => x.ActualTime).GreaterThanOrEqualTo(0).When(x => x.ActualTime.HasValue);
        RuleFor(x => x.DueAt)
            .GreaterThanOrEqualTo(x => x.StartAt!.Value)
            .When(x => x.StartAt.HasValue && x.DueAt.HasValue)
            .WithMessage("Due date must be on or after the start date.");
    }
}

internal sealed class CreateNoteRequestValidator : AbstractValidator<CreateNoteRequest>
{
    public CreateNoteRequestValidator() => RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
}

internal sealed class CreateDocumentRequestValidator : AbstractValidator<CreateDocumentRequest>
{
    public CreateDocumentRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.FilePath).NotEmpty();
    }
}

internal sealed class CreateDailyReportRequestValidator : AbstractValidator<CreateDailyReportRequest>
{
    public CreateDailyReportRequestValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.EstimatedTime).GreaterThanOrEqualTo(0).When(x => x.EstimatedTime.HasValue);
        RuleFor(x => x.ActualTime).GreaterThanOrEqualTo(0).When(x => x.ActualTime.HasValue);
        RuleFor(x => x.RemainingTime).GreaterThanOrEqualTo(0).When(x => x.RemainingTime.HasValue);
    }
}

internal sealed class UpdateDailyReportRequestValidator : AbstractValidator<UpdateDailyReportRequest>
{
    public UpdateDailyReportRequestValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.EstimatedTime).GreaterThanOrEqualTo(0).When(x => x.EstimatedTime.HasValue);
        RuleFor(x => x.ActualTime).GreaterThanOrEqualTo(0).When(x => x.ActualTime.HasValue);
        RuleFor(x => x.RemainingTime).GreaterThanOrEqualTo(0).When(x => x.RemainingTime.HasValue);
    }
}

internal sealed class CreateStatusRequestValidator : AbstractValidator<CreateStatusRequest>
{
    public CreateStatusRequestValidator()
    {
        RuleFor(x => x.StatusTypeCode).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Color).MaximumLength(20);
    }
}

internal sealed class UpdateStatusRequestValidator : AbstractValidator<UpdateStatusRequest>
{
    public UpdateStatusRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Color).MaximumLength(20);
    }
}
