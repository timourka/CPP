using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace WebAppServer.Services
{
    public class CourseReportService : ICourseReportService
    {
        private readonly AppDbContext _db;

        public CourseReportService(AppDbContext db)
        {
            _db = db;
        }

        public async System.Threading.Tasks.Task<CourseReportGenerationResult> GenerateCourseReportAsync(int courseId, string requestedByLogin, CourseReportFormat format)
        {
            var courseDataResult = await BuildCourseDataAsync(courseId, requestedByLogin);
            if (courseDataResult.Status != CourseReportGenerationStatus.Success || courseDataResult.Data == null)
            {
                return new CourseReportGenerationResult
                {
                    Status = courseDataResult.Status
                };
            }

            var safeCourseName = SanitizeFileName(courseDataResult.Data.CourseName);
            var timestampSuffix = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            return format switch
            {
                CourseReportFormat.Pdf => new CourseReportGenerationResult
                {
                    Status = CourseReportGenerationStatus.Success,
                    File = new CourseReportFile
                    {
                        FileName = $"course-report-{safeCourseName}-{timestampSuffix}.pdf",
                        ContentType = "application/pdf",
                        Content = BuildPdf(courseDataResult.Data)
                    }
                },
                CourseReportFormat.Excel => new CourseReportGenerationResult
                {
                    Status = CourseReportGenerationStatus.Success,
                    File = new CourseReportFile
                    {
                        FileName = $"course-report-{safeCourseName}-{timestampSuffix}.xlsx",
                        ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        Content = BuildExcel(courseDataResult.Data)
                    }
                },
                _ => new CourseReportGenerationResult { Status = CourseReportGenerationStatus.NotFound }
            };
        }

        private async System.Threading.Tasks.Task<CourseDataResult> BuildCourseDataAsync(int courseId, string requestedByLogin)
        {
            var course = await _db.Courses
                .Include(c => c.Avtors)
                .Include(c => c.Participants)
                .Include(c => c.Tasks)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
            {
                return new CourseDataResult { Status = CourseReportGenerationStatus.NotFound };
            }

            if (!course.Avtors.Any(a => a.Login == requestedByLogin))
            {
                return new CourseDataResult { Status = CourseReportGenerationStatus.Forbidden };
            }

            var answers = await _db.Answers
                .Include(a => a.Task)!.ThenInclude(t => t!.Course)
                .Where(a => a.Task != null && a.Task.Course != null && a.Task.Course.Id == courseId)
                .ToListAsync();

            var reviewRequests = await _db.ReviewRequests
                .Include(r => r.Answer)!.ThenInclude(a => a.Task)!.ThenInclude(t => t!.Course)
                .Where(r => r.Answer != null && r.Answer.Task != null && r.Answer.Task.Course != null && r.Answer.Task.Course.Id == courseId)
                .ToListAsync();

            var reviewComments = await _db.ReviewComments
                .Include(c => c.Answer)!.ThenInclude(a => a.Task)!.ThenInclude(t => t!.Course)
                .Where(c => c.Answer != null && c.Answer.Task != null && c.Answer.Task.Course != null && c.Answer.Task.Course.Id == courseId)
                .ToListAsync();

            var taskReports = new List<TaskReportData>();
            foreach (var courseTask in course.Tasks.OrderBy(t => t.Id))
            {
                var taskAnswers = answers.Where(a => a.Task?.Id == courseTask.Id).ToList();
                var gradedAnswers = taskAnswers.Where(a => a.Grade >= 0).ToList();
                var taskReviewRequests = reviewRequests.Where(r => r.Answer?.Task?.Id == courseTask.Id).ToList();
                var taskReviewComments = reviewComments.Where(c => c.Answer?.Task?.Id == courseTask.Id).ToList();

                var taskReport = new TaskReportData
                {
                    TaskId = courseTask.Id,
                    TaskName = string.IsNullOrWhiteSpace(courseTask.Name) ? $"Задание #{courseTask.Id}" : courseTask.Name,
                    Answers = taskAnswers.Count,
                    Drafts = taskAnswers.Count(a => a.Status == "Черновик"),
                    AwaitingReview = taskAnswers.Count(a => a.Status == "Ожидает проверки"),
                    Reviewed = taskAnswers.Count(a => a.Status == "Проверено"),
                    ReviewRequests = taskReviewRequests.Count,
                    CompletedReviews = taskReviewRequests.Count(r => r.Completed),
                    ReviewComments = taskReviewComments.Count,
                    AverageGrade = gradedAnswers.Count > 0 ? Math.Round(gradedAnswers.Average(a => a.Grade), 2) : null
                };

                taskReports.Add(taskReport);
            }

            var gradedCourseAnswers = answers.Where(a => a.Grade >= 0).ToList();
            var data = new CourseReportData
            {
                CourseName = string.IsNullOrWhiteSpace(course.Name) ? $"Курс #{course.Id}" : course.Name,
                CourseDescription = course.Description ?? string.Empty,
                ParticipantsCount = course.Participants.Count,
                TasksCount = course.Tasks.Count,
                TotalAnswers = taskReports.Sum(t => t.Answers),
                AwaitingReview = taskReports.Sum(t => t.AwaitingReview),
                Reviewed = taskReports.Sum(t => t.Reviewed),
                Drafts = taskReports.Sum(t => t.Drafts),
                ReviewRequests = taskReports.Sum(t => t.ReviewRequests),
                CompletedReviews = taskReports.Sum(t => t.CompletedReviews),
                ReviewComments = taskReports.Sum(t => t.ReviewComments),
                AverageGrade = gradedCourseAnswers.Count > 0 ? Math.Round(gradedCourseAnswers.Average(a => a.Grade), 2) : null,
                GeneratedAtUtc = DateTime.UtcNow,
                Tasks = taskReports
            };

            return new CourseDataResult
            {
                Status = CourseReportGenerationStatus.Success,
                Data = data
            };
        }

        private byte[] BuildPdf(CourseReportData data)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.DefaultTextStyle(TextStyle.Default.FontSize(11));

                    page.Header()
                        .Text($"Отчет по курсу \"{data.CourseName}\"")
                        .FontSize(20)
                        .SemiBold();

                    page.Content().PaddingTop(10).Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Text(text =>
                        {
                            text.Span("Дата формирования: ").SemiBold();
                            text.Span(data.GeneratedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm"));
                        });

                        if (!string.IsNullOrWhiteSpace(data.CourseDescription))
                        {
                            column.Item().Text(text =>
                            {
                                text.Span("Описание: ").SemiBold();
                                text.Span(data.CourseDescription);
                            });
                        }

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text(text =>
                            {
                                text.Span("Участников: ").SemiBold();
                                text.Span(data.ParticipantsCount.ToString());
                            });

                            row.RelativeItem().Text(text =>
                            {
                                text.Span("Заданий: ").SemiBold();
                                text.Span(data.TasksCount.ToString());
                            });
                        });

                        column.Item().Text("Сводка по ответам").FontSize(14).SemiBold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn();
                            });

                            AddSummaryRow(table, "Всего ответов", data.TotalAnswers.ToString());
                            AddSummaryRow(table, "Черновики", data.Drafts.ToString());
                            AddSummaryRow(table, "Ожидает проверки", data.AwaitingReview.ToString());
                            AddSummaryRow(table, "Проверено", data.Reviewed.ToString());
                            AddSummaryRow(table, "Запросы на ревью", data.ReviewRequests.ToString());
                            AddSummaryRow(table, "Завершенные ревью", data.CompletedReviews.ToString());
                            AddSummaryRow(table, "Комментариев", data.ReviewComments.ToString());
                            AddSummaryRow(table, "Средняя оценка", data.AverageGrade?.ToString("0.##") ?? "-");
                        });

                        column.Item().PaddingTop(5).Text("Детализация по заданиям").FontSize(14).SemiBold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(20);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellHeader).Text("#");
                                header.Cell().Element(CellHeader).Text("Задание");
                                header.Cell().Element(CellHeader).Text("Ответов");
                                header.Cell().Element(CellHeader).Text("Черновики");
                                header.Cell().Element(CellHeader).Text("Ожидает проверки");
                                header.Cell().Element(CellHeader).Text("Проверено");
                                header.Cell().Element(CellHeader).Text("Запросы на ревью");
                                header.Cell().Element(CellHeader).Text("Завершенные ревью");
                                header.Cell().Element(CellHeader).Text("Комментариев");
                                header.Cell().Element(CellHeader).Text("Средняя оценка");
                            });

                            var index = 1;
                            foreach (var taskReport in data.Tasks)
                            {
                                table.Cell().Element(CellContent).Text(index.ToString());
                                table.Cell().Element(CellContent).Text(taskReport.TaskName);
                                table.Cell().Element(CellContent).Text(taskReport.Answers.ToString());
                                table.Cell().Element(CellContent).Text(taskReport.Drafts.ToString());
                                table.Cell().Element(CellContent).Text(taskReport.AwaitingReview.ToString());
                                table.Cell().Element(CellContent).Text(taskReport.Reviewed.ToString());
                                table.Cell().Element(CellContent).Text(taskReport.ReviewRequests.ToString());
                                table.Cell().Element(CellContent).Text(taskReport.CompletedReviews.ToString());
                                table.Cell().Element(CellContent).Text(taskReport.ReviewComments.ToString());
                                table.Cell().Element(CellContent).Text(taskReport.AverageGrade?.ToString("0.##") ?? "-");
                                index++;
                            }
                        });
                    });

                    page.Footer().Element(footer =>
                    {
                        footer.AlignCenter();
                        footer.DefaultTextStyle(TextStyle.Default.FontSize(9).FontColor(Colors.Grey.Medium));
                        footer.Text(x =>
                        {
                            x.Span("Сформировано автоматически — ");
                            x.Span("TaskReviewPlatform").SemiBold();
                        });
                    });
                });
            }).GeneratePdf();
        }

        private static void AddSummaryRow(TableDescriptor table, string label, string value)
        {
            table.Cell().PaddingVertical(2).Text(label);
            table.Cell().PaddingVertical(2).Text(value).SemiBold();
        }

        private static IContainer CellHeader(IContainer container)
        {
            container.DefaultTextStyle(TextStyle.Default.SemiBold());
            return container
                .Padding(3)
                .Background(Colors.Grey.Lighten2)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Medium);
        }

        private static IContainer CellContent(IContainer container)
        {
            return container.Padding(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
        }

        private byte[] BuildExcel(CourseReportData data)
        {
            using var workbook = new XLWorkbook();
            var summarySheet = workbook.Worksheets.Add("Сводка");

            summarySheet.Cell("A1").Value = "Курс";
            summarySheet.Cell("B1").Value = data.CourseName;
            summarySheet.Cell("A2").Value = "Описание";
            summarySheet.Cell("B2").Value = data.CourseDescription;
            summarySheet.Cell("A3").Value = "Участников";
            summarySheet.Cell("B3").Value = data.ParticipantsCount;
            summarySheet.Cell("A4").Value = "Заданий";
            summarySheet.Cell("B4").Value = data.TasksCount;
            summarySheet.Cell("A5").Value = "Всего ответов";
            summarySheet.Cell("B5").Value = data.TotalAnswers;
            summarySheet.Cell("A6").Value = "Черновики";
            summarySheet.Cell("B6").Value = data.Drafts;
            summarySheet.Cell("A7").Value = "Ожидает проверки";
            summarySheet.Cell("B7").Value = data.AwaitingReview;
            summarySheet.Cell("A8").Value = "Проверено";
            summarySheet.Cell("B8").Value = data.Reviewed;
            summarySheet.Cell("A9").Value = "Запросы на ревью";
            summarySheet.Cell("B9").Value = data.ReviewRequests;
            summarySheet.Cell("A10").Value = "Завершенные ревью";
            summarySheet.Cell("B10").Value = data.CompletedReviews;
            summarySheet.Cell("A11").Value = "Комментариев";
            summarySheet.Cell("B11").Value = data.ReviewComments;
            summarySheet.Cell("A12").Value = "Средняя оценка";
            summarySheet.Cell("B12").Value = data.AverageGrade?.ToString("0.00") ?? "-";
            summarySheet.Cell("A13").Value = "Дата формирования (UTC)";
            summarySheet.Cell("B13").Value = data.GeneratedAtUtc;
            summarySheet.Cell("B13").Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
            summarySheet.Columns().AdjustToContents();

            var tasksSheet = workbook.Worksheets.Add("Задания");
            var headers = new List<string>
            {
                "#", "Задание", "Ответов", "Черновики", "Ожидает проверки", "Проверено", "Запросы на ревью",
                "Завершенные ревью", "Комментариев", "Средняя оценка"
            };

            for (var i = 0; i < headers.Count; i++)
            {
                tasksSheet.Cell(1, i + 1).Value = headers[i];
                tasksSheet.Cell(1, i + 1).Style.Font.Bold = true;
                tasksSheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.AliceBlue;
            }

            var rowIndex = 2;
            var index = 1;
            foreach (var task in data.Tasks)
            {
                tasksSheet.Cell(rowIndex, 1).Value = index;
                tasksSheet.Cell(rowIndex, 2).Value = task.TaskName;
                tasksSheet.Cell(rowIndex, 3).Value = task.Answers;
                tasksSheet.Cell(rowIndex, 4).Value = task.Drafts;
                tasksSheet.Cell(rowIndex, 5).Value = task.AwaitingReview;
                tasksSheet.Cell(rowIndex, 6).Value = task.Reviewed;
                tasksSheet.Cell(rowIndex, 7).Value = task.ReviewRequests;
                tasksSheet.Cell(rowIndex, 8).Value = task.CompletedReviews;
                tasksSheet.Cell(rowIndex, 9).Value = task.ReviewComments;
                tasksSheet.Cell(rowIndex, 10).Value = task.AverageGrade?.ToString("0.00") ?? "-";
                rowIndex++;
                index++;
            }

            tasksSheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static string SanitizeFileName(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "report";
            }

            var invalid = Path.GetInvalidFileNameChars();
            var safeChars = input.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
            return new string(safeChars);
        }

        private class CourseDataResult
        {
            public CourseReportGenerationStatus Status { get; init; }

            public CourseReportData? Data { get; init; }
        }
    }
}
