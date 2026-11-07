using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Business.Fee.DTOs;

/// <summary>One class's fee structure, as shown and edited on the Fee Structure page.</summary>
public record ClassFeeRateDto(
    string ClassId, string ClassName, int Order,
    decimal AdmissionFee, decimal MonthlyTuition,
    decimal QuarterlyExamFee, decimal HalfYearlyExamFee, decimal AnnualExamFee,
    DateTime? UpdatedAtUtc);

public record ClassFeeRateInput(
    [Required, Display(Name = "Class")] string ClassId,
    [Range(0, 1_000_000), Display(Name = "Admission fee")] decimal AdmissionFee,
    [Range(0, 1_000_000), Display(Name = "Monthly tuition fee")] decimal MonthlyTuition,
    [Range(0, 1_000_000), Display(Name = "Quarterly exam fee")] decimal QuarterlyExamFee,
    [Range(0, 1_000_000), Display(Name = "Half-yearly exam fee")] decimal HalfYearlyExamFee,
    [Range(0, 1_000_000), Display(Name = "Annual exam fee")] decimal AnnualExamFee);

public record UpdateClassFeeRatesRequest([Required, MinLength(1)] IReadOnlyList<ClassFeeRateInput> Rates);

public record FeeScheduleDto(
    [Range(1, 28), Display(Name = "Monthly due day")] int MonthlyDueDay,
    [Range(1, 12), Display(Name = "Quarterly exam due month")] int QuarterlyExamDueMonth,
    [Range(1, 31), Display(Name = "Quarterly exam due day")] int QuarterlyExamDueDay,
    [Range(1, 12), Display(Name = "Half-yearly exam due month")] int HalfYearlyExamDueMonth,
    [Range(1, 31), Display(Name = "Half-yearly exam due day")] int HalfYearlyExamDueDay,
    [Range(1, 12), Display(Name = "Annual exam due month")] int AnnualExamDueMonth,
    [Range(1, 31), Display(Name = "Annual exam due day")] int AnnualExamDueDay);
