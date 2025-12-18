using System;
using System.Collections.Generic;

namespace WebAppServer.Services
{
    public interface ICourseReportService
    {
        System.Threading.Tasks.Task<CourseReportGenerationResult> GenerateCourseReportAsync(int courseId, string requestedByLogin, CourseReportFormat format);
    }

    public enum CourseReportFormat
    {
        Pdf,
        Excel
    }

    public enum CourseReportGenerationStatus
    {
        Success,
        NotFound,
        Forbidden
    }

    public class CourseReportGenerationResult
    {
        public CourseReportGenerationStatus Status { get; init; }

        public CourseReportFile? File { get; init; }
    }

    public class CourseReportFile
    {
        public required string FileName { get; init; }

        public required string ContentType { get; init; }

        public required byte[] Content { get; init; }
    }

    public class CourseReportData
    {
        public required string CourseName { get; init; }

        public string CourseDescription { get; init; } = string.Empty;

        public int ParticipantsCount { get; init; }

        public int TasksCount { get; init; }

        public int TotalAnswers { get; init; }

        public int AwaitingReview { get; init; }

        public int Reviewed { get; init; }

        public int Drafts { get; init; }

        public int ReviewRequests { get; init; }

        public int CompletedReviews { get; init; }

        public int ReviewComments { get; init; }

        public double? AverageGrade { get; init; }

        public DateTime GeneratedAtUtc { get; init; }

        public List<TaskReportData> Tasks { get; init; } = new();
    }

    public class TaskReportData
    {
        public int TaskId { get; init; }

        public string TaskName { get; init; } = string.Empty;

        public int Answers { get; init; }

        public int Drafts { get; init; }

        public int AwaitingReview { get; init; }

        public int Reviewed { get; init; }

        public int ReviewRequests { get; init; }

        public int CompletedReviews { get; init; }

        public int ReviewComments { get; init; }

        public double? AverageGrade { get; init; }
    }
}
