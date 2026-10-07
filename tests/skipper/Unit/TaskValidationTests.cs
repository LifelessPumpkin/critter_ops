using System.ComponentModel.DataAnnotations;
using skipper_api.Domain.Tasks;
using skipper_api.Dtos.Tasks;
using Xunit;

namespace MaryAnn.Skipper.UnitTests;

public class TaskValidationTests
{
    [Theory]
    [InlineData(RecurrenceType.Custom, null, false)]
    [InlineData(RecurrenceType.Custom, 3, true)]
    [InlineData(RecurrenceType.None, null, true)]
    [InlineData(RecurrenceType.None, 3, false)]
    [InlineData(RecurrenceType.Daily, 0, false)]
    public void RecurrenceRequiresValidInterval(RecurrenceType recurrence, int? interval, bool expected)
    {
        var request = new CreateTaskRequestDto
        {
            Title = "Feed Mango", TaskType = TaskType.Feeding, DueAt = DateTime.UtcNow,
            RecurrenceType = recurrence, RecurrenceInterval = interval,
        };
        var errors = new List<ValidationResult>();
        Assert.Equal(expected, Validator.TryValidateObject(request, new ValidationContext(request), errors, true));
        if (!expected) Assert.Contains(errors, error => error.MemberNames.Contains(nameof(request.RecurrenceInterval)));
    }
}
