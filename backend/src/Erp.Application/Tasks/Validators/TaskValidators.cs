using Erp.Application.Tasks.Contracts;
using FluentValidation;

namespace Erp.Application.Tasks.Validators;

public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.EstimatedHours).GreaterThanOrEqualTo(0).When(x => x.EstimatedHours.HasValue);
        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .When(x => x.StartDate.HasValue && x.DueDate.HasValue)
            .WithMessage("Due date must be on or after the start date.");
    }
}

public sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.EstimatedHours).GreaterThanOrEqualTo(0).When(x => x.EstimatedHours.HasValue);
        RuleFor(x => x.ActualHours).GreaterThanOrEqualTo(0).When(x => x.ActualHours.HasValue);
        RuleFor(x => x.CompletionPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .When(x => x.StartDate.HasValue && x.DueDate.HasValue)
            .WithMessage("Due date must be on or after the start date.");
    }
}

public sealed class CreateStatusTypeRequestValidator : AbstractValidator<CreateStatusTypeRequest>
{
    public CreateStatusTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class UpdateStatusTypeRequestValidator : AbstractValidator<UpdateStatusTypeRequest>
{
    public UpdateStatusTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class CreateStatusRequestValidator : AbstractValidator<CreateStatusRequest>
{
    public CreateStatusRequestValidator()
    {
        RuleFor(x => x.StatusTypeId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Color).MaximumLength(20);
    }
}

public sealed class UpdateStatusRequestValidator : AbstractValidator<UpdateStatusRequest>
{
    public UpdateStatusRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Color).MaximumLength(20);
    }
}

public sealed class CreateChecklistItemRequestValidator : AbstractValidator<CreateChecklistItemRequest>
{
    public CreateChecklistItemRequestValidator() => RuleFor(x => x.Text).NotEmpty().MaximumLength(500);
}

public sealed class UpdateChecklistItemRequestValidator : AbstractValidator<UpdateChecklistItemRequest>
{
    public UpdateChecklistItemRequestValidator() => RuleFor(x => x.Text).NotEmpty().MaximumLength(500);
}

public sealed class CreateNoteRequestValidator : AbstractValidator<CreateNoteRequest>
{
    public CreateNoteRequestValidator() => RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
}

public sealed class UpdateNoteRequestValidator : AbstractValidator<UpdateNoteRequest>
{
    public UpdateNoteRequestValidator() => RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
}

public sealed class CreateDocumentRequestValidator : AbstractValidator<CreateDocumentRequest>
{
    public CreateDocumentRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.FileType).MaximumLength(60);
        RuleFor(x => x.Url).MaximumLength(2000);
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}

public sealed class CreateDependencyRequestValidator : AbstractValidator<CreateDependencyRequest>
{
    public CreateDependencyRequestValidator() => RuleFor(x => x.DependsOnTaskId).NotEmpty();
}

public sealed class CreateRelationRequestValidator : AbstractValidator<CreateRelationRequest>
{
    public CreateRelationRequestValidator()
    {
        RuleFor(x => x.RelatedEntityType).NotEmpty().MaximumLength(60);
        RuleFor(x => x.RelatedEntityId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
