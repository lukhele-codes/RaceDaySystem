using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests;

public class EnrolmentsControllerTests
{
    [Fact]
    public async Task Enrol_ValidCategory_ReturnsCreated()
    {
        using var db = TestHelper.NewDb();
        _ = db.Categories.Add(new Category
        {
            CategoryId = 1,
            EventId = 1,
            CategoryName = "10km",
            DistanceKm = 10,
            MaxParticipants = 5,
            EntryFee = 100
        });
        await db.SaveChangesAsync();

        var controller = new EnrolmentsController(db);
        TestHelper.SetUser(controller, 10, "Participant");

        var response = await controller.Enrol(new EnrolmentWriteDto { CategoryId = 1 });

        Assert.IsType<CreatedAtActionResult>(response.Result);
    }

    [Fact]
    public async Task Enrol_Twice_ReturnsConflict()
    {
        using var db = TestHelper.NewDb();
        _ = db.Categories.Add(new Category
        {
            CategoryId = 1,
            EventId = 1,
            CategoryName = "10km",
            DistanceKm = 10,
            MaxParticipants = 5,
            EntryFee = 100
        });
        await db.SaveChangesAsync();

        var controller = new EnrolmentsController(db);
        TestHelper.SetUser(controller, 10, "Participant");

        await controller.Enrol(new EnrolmentWriteDto { CategoryId = 1 });
        var second = await controller.Enrol(new EnrolmentWriteDto { CategoryId = 1 });

        Assert.IsType<ConflictObjectResult>(second.Result);
    }

    [Fact]
    public async Task Enrol_FullCategory_ReturnsConflict()
    {
        using var db = TestHelper.NewDb();
        db.Categories.Add(new Category
        {
            CategoryId = 1,
            EventId = 1,
            CategoryName = "10km",
            DistanceKm = 10,
            MaxParticipants = 1,
            EntryFee = 100
        });
        // Someone else already took the only spot
        db.Enrolments.Add(new Enrolment
        {
            ParticipantId = 99,
            CategoryId = 1,
            EnrolmentDate = DateTime.UtcNow,
            Status = "Confirmed"
        });
        await db.SaveChangesAsync();

        var controller = new EnrolmentsController(db);
        TestHelper.SetUser(controller, 10, "Participant");

        var response = await controller.Enrol(new EnrolmentWriteDto { CategoryId = 1 });

        Assert.IsType<ConflictObjectResult>(response.Result);
    }
}