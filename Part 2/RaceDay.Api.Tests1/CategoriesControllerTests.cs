using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests;

public class CategoriesControllerTests
{
    private static CategoryWriteDto NewCategory() => new()
    {
        Name = "10km",
        DistanceKm = 10,
        MaxParticipants = 100,
        EntryFee = 150
    };

    [Fact]
    public async Task Create_OwningOrganiser_ReturnsCreated()
    {
        using var db = TestHelper.NewDb();
        db.Events.Add(new Event { EventId = 1, EventName = "Fuller Run", OrganiserId = 5 });
        await db.SaveChangesAsync();

        var controller = new CategoriesController(db);
        TestHelper.SetUser(controller, 5, "Organiser");

        var response = await controller.CreateCategory(1, NewCategory());

        Assert.IsType<CreatedAtActionResult>(response.Result);
    }

    [Fact]
    public async Task Create_OtherOrganiser_ReturnsForbid()
    {
        using var db = TestHelper.NewDb();
        db.Events.Add(new Event { EventId = 1, EventName = "Fuller Run", OrganiserId = 5 });
        await db.SaveChangesAsync();

        var controller = new CategoriesController(db);
        TestHelper.SetUser(controller, 6, "Organiser");   // not the owner

        var response = await controller.CreateCategory(1, NewCategory());

        Assert.IsType<ForbidResult>(response.Result);
    }

    [Fact]
    public async Task Create_Admin_CanCreateOnAnyEvent()
    {
        using var db = TestHelper.NewDb();
        db.Events.Add(new Event { EventId = 1, EventName = "Fuller Run", OrganiserId = 5 });
        await db.SaveChangesAsync();

        var controller = new CategoriesController(db);
        TestHelper.SetUser(controller, 1, "Admin");

        var response = await controller.CreateCategory(1, NewCategory());

        Assert.IsType<CreatedAtActionResult>(response.Result);
    }

    [Fact]
    public async Task Create_EventDoesNotExist_ReturnsNotFound()
    {
        using var db = TestHelper.NewDb();

        var controller = new CategoriesController(db);
        TestHelper.SetUser(controller, 5, "Organiser");

        var response = await controller.CreateCategory(999, NewCategory());

        Assert.IsType<NotFoundObjectResult>(response.Result);
    }

    [Fact]
    public async Task Delete_OtherOrganisersCategory_ReturnsForbid()
    {
        using var db = TestHelper.NewDb();
        db.Events.Add(new Event { EventId = 1, EventName = "Fuller Run", OrganiserId = 5 });
        db.Categories.Add(new Category
        {
            CategoryId = 1,
            EventId = 1,
            CategoryName = "10km",
            DistanceKm = 10,
            MaxParticipants = 100,
            EntryFee = 150
        });
        await db.SaveChangesAsync();

        var controller = new CategoriesController(db);
        TestHelper.SetUser(controller, 6, "Organiser");

        var response = await controller.DeleteCategory(1, 1);
        Assert.IsType<ForbidResult>(response);
    }
}