using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BackendApi.Endpoints.Contacts;

/// <summary>
/// Request model for searching contact users.
/// </summary>
public class SearchContactsRequest
{
    /// <summary>
    /// First name to search for.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name to search for.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Job title to search for.
    /// </summary>
    public string? JobTitle { get; set; }
}

/// <summary>
/// Endpoint to search for contact users based on various criteria.
/// </summary>
public class SearchContactsEndpoint : Endpoint<SearchContactsRequest, List<ContactUser>>
{
    private readonly IContactUserRepository _repository;
    private readonly ILogger<SearchContactsEndpoint> _logger;

    public SearchContactsEndpoint(IContactUserRepository repository, ILogger<SearchContactsEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/search/contacts");
        Policies("ReadAccess");
        Options(x => x
            .WithTags("Contacts")
            .WithSummary("Search contact users")
            .WithDescription("Search for contact users by first name, last name, or job title")
            .Produces<List<ContactUser>>(200, "application/json")
            .Produces(StatusCodes.Status500InternalServerError, typeof(ProblemDetails)));
    }

    public override async Task HandleAsync(SearchContactsRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Searching contact users with criteria - FirstName: {FirstName}, LastName: {LastName}, JobTitle: {JobTitle}",
                req.FirstName ?? "N/A", 
                req.LastName ?? "N/A", 
                req.JobTitle ?? "N/A");
            
            var contacts = await _repository.SearchAsync(
                req.FirstName, 
                req.LastName, 
                req.JobTitle, 
                ct);
            
            _logger.LogInformation("Search returned {Count} contact users", contacts.Count);
            
            await SendAsync(contacts, cancellation: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while searching contact users");
            
            await SendAsync(new List<ContactUser>(), 500, ct);
        }
    }
}
