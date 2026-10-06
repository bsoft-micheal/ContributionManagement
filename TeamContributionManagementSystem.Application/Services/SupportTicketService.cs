using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.SupportTickets;
using TeamContributionManagementSystem.Application.DTOs.Users;
using TeamContributionManagementSystem.Application.Interfaces.Common;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class SupportTicketService : ISupportTicketService
{
    private readonly ILogger<SupportTicketService> _logger;
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly IStatusRepository? _statusRepository;
    private readonly ITicketTypeRepository? _ticketTypeRepository;
    private readonly IPriorityRepository? _priorityRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService? _currentUserService;
    private readonly IMemberRepository? _memberRepository;
    private readonly IEventRepository? _eventRepository;
    private readonly IEmailService? _emailService;
    private readonly IUserRepository? _userRepository;
    private readonly IRoleRepository? _roleRepository;
    private readonly IConfiguration? _configuration;

    public SupportTicketService(
        ILogger<SupportTicketService> logger,
        ISupportTicketRepository ticketRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IStatusRepository? statusRepository = null,
        ITicketTypeRepository? ticketTypeRepository = null,
        IPriorityRepository? priorityRepository = null,
        ICurrentUserService? currentUserService = null,
        IMemberRepository? memberRepository = null,
        IEventRepository? eventRepository = null,
        IEmailService? emailService = null,
        IUserRepository? userRepository = null,
        IRoleRepository? roleRepository = null,
        IConfiguration? configuration = null)
    {
        _logger = logger;
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _statusRepository = statusRepository;
        _ticketTypeRepository = ticketTypeRepository;
        _priorityRepository = priorityRepository;
        _currentUserService = currentUserService;
        _memberRepository = memberRepository;
        _eventRepository = eventRepository;
        _emailService = emailService;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _configuration = configuration;
    }

    public async Task<IReadOnlyCollection<SupportTicketDto>> GetAllAsync(string? status = null, string? ticketType = null, string? priority = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var all = await _ticketRepository.GetAllAsync(status, ticketType, priority, cancellationToken);
            if (_currentUserService != null && _currentUserService.IsMemberRole)
            {
                var myMemberId = _currentUserService.MemberId;
                Domain.Entities.Member? myMember = null;
                if (myMemberId.HasValue && _memberRepository != null)
                {
                    myMember = await _memberRepository.GetByIdAsync(myMemberId.Value, cancellationToken);
                }

                var userEmail = _currentUserService.Email;
                if (myMember == null && !string.IsNullOrWhiteSpace(userEmail) && _memberRepository != null)
                {
                    myMember = await _memberRepository.GetByEmailAsync(userEmail.Trim(), cancellationToken);
                }

                var myMemberIdStr = myMember?.MemberId.ToString();
                var myMemberName = myMember?.Name.Trim();
                var myUserEmail = userEmail?.Trim();

                return all.Where(t =>
                    (myMemberIdStr != null && string.Equals(t.MemberId, myMemberIdStr, StringComparison.OrdinalIgnoreCase)) ||
                    (myMemberName != null && string.Equals(t.MemberName, myMemberName, StringComparison.OrdinalIgnoreCase)) ||
                    (myMemberName != null && string.Equals(t.CreatedBy, myMemberName, StringComparison.OrdinalIgnoreCase)) ||
                    (myUserEmail != null && string.Equals(t.CreatedBy, myUserEmail, StringComparison.OrdinalIgnoreCase)) ||
                    (myMemberIdStr != null && string.Equals(t.CreatedBy, myMemberIdStr, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }
            return all;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    public async Task<SupportTicketDto> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        try
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.SupportTickets.NotFound);

            if (_currentUserService != null && _currentUserService.IsMemberRole)
            {
                var myMemberId = _currentUserService.MemberId;
                Domain.Entities.Member? myMember = null;
                if (myMemberId.HasValue && _memberRepository != null)
                {
                    myMember = await _memberRepository.GetByIdAsync(myMemberId.Value, cancellationToken);
                }

                var userEmail = _currentUserService.Email;
                if (myMember == null && !string.IsNullOrWhiteSpace(userEmail) && _memberRepository != null)
                {
                    myMember = await _memberRepository.GetByEmailAsync(userEmail.Trim(), cancellationToken);
                }

                var myMemberIdStr = myMember?.MemberId.ToString();
                var myMemberName = myMember?.Name.Trim();
                var myUserEmail = userEmail?.Trim();

                bool isOwner =
                    (myMemberIdStr != null && string.Equals(ticket.MemberId, myMemberIdStr, StringComparison.OrdinalIgnoreCase)) ||
                    (myMemberName != null && string.Equals(ticket.MemberName, myMemberName, StringComparison.OrdinalIgnoreCase)) ||
                    (ticket.UserId.HasValue && myMember != null && ticket.UserId.Value == myMember.MemberId) ||
                    (ticket.CreatedBy.HasValue && myMember != null && ticket.CreatedBy.Value == myMember.MemberId) ||
                    (ticket.CreatedBy.HasValue && !string.IsNullOrWhiteSpace(myMemberIdStr) && ticket.CreatedBy.Value.ToString().Equals(myMemberIdStr, StringComparison.OrdinalIgnoreCase));

                if (!isOwner)
                {
                    throw new UnauthorizedAccessException("Access denied to requested support ticket.");
                }
            }

            return _mapper.Map<SupportTicketDto>(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdAsync));
            throw;
        }
    }

    public async Task<SupportTicketDto> CreateAsync(CreateSupportTicketRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var ticketNo = await _ticketRepository.GenerateNextTicketNoAsync(cancellationToken);

            var ticketType = request.TicketType?.Trim();
            Guid? ticketTypeId = null;
            if (_ticketTypeRepository != null)
            {
                if (string.IsNullOrWhiteSpace(ticketType))
                {
                    var dbTicketTypes = await _ticketTypeRepository.GetAllAsync(true, cancellationToken);
                    var firstType = dbTicketTypes.FirstOrDefault();
                    ticketType = firstType?.TypeName ?? string.Empty;
                    ticketTypeId = firstType?.TicketTypeId;
                }
                else
                {
                    var matched = await _ticketTypeRepository.GetByNameAsync(ticketType, cancellationToken);
                    ticketTypeId = matched?.TicketTypeId;
                }
            }

            var status = request.Status?.Trim();
            Guid? statusId = null;
            if (_statusRepository != null)
            {
                if (string.IsNullOrWhiteSpace(status))
                {
                    var dbStatuses = await _statusRepository.GetAllAsync(true, cancellationToken);
                    var matched = dbStatuses.FirstOrDefault(s => s.StatusName.Equals("Open", StringComparison.OrdinalIgnoreCase))
                        ?? dbStatuses.FirstOrDefault();
                    status = matched?.StatusName ?? "Open";
                    statusId = matched?.StatusId;
                }
                else
                {
                    var matched = await _statusRepository.GetByNameAsync(status, cancellationToken);
                    statusId = matched?.StatusId;
                }
            }
            else if (string.IsNullOrWhiteSpace(status))
            {
                status = "Open";
            }

            var priority = request.Priority?.Trim();
            Guid? priorityId = null;
            if (_priorityRepository != null)
            {
                if (string.IsNullOrWhiteSpace(priority))
                {
                    var dbPriorities = await _priorityRepository.GetAllAsync(true, cancellationToken);
                    var matched = dbPriorities.FirstOrDefault(p => p.PriorityName.Equals("Medium", StringComparison.OrdinalIgnoreCase))
                        ?? dbPriorities.FirstOrDefault();
                    priority = matched?.PriorityName ?? "Medium";
                    priorityId = matched?.PriorityId;
                }
                else
                {
                    var matched = await _priorityRepository.GetByNameAsync(priority, cancellationToken);
                    priorityId = matched?.PriorityId;
                }
            }
            else if (string.IsNullOrWhiteSpace(priority))
            {
                priority = "Medium";
            }

            var refNo = request.RefNo?.Trim();
            if (string.IsNullOrWhiteSpace(refNo))
            {
                refNo = await _ticketRepository.GenerateNextRefNoAsync(cancellationToken);
            }

            Guid? userId = null;
            if (!string.IsNullOrWhiteSpace(request.MemberId) && Guid.TryParse(request.MemberId, out var parsedGuid))
            {
                userId = parsedGuid;
            }
            else if (!string.IsNullOrWhiteSpace(request.MemberName) && _memberRepository != null)
            {
                var userObj = await _memberRepository.GetUserByNameAsync(request.MemberName.Trim(), cancellationToken);
                userId = userObj?.UserId;
            }
            if (userId == null && _currentUserService != null)
            {
                if (_currentUserService.MemberId.HasValue)
                {
                    userId = _currentUserService.MemberId.Value;
                }
                else if (Guid.TryParse(_currentUserService.UserId, out var curUserGuid))
                {
                    userId = curUserGuid;
                }
            }

            Guid? eventId = null;
            if (!string.IsNullOrWhiteSpace(request.RelatedEvent) && _eventRepository != null)
            {
                var eventObj = await _eventRepository.GetByNameAsync(request.RelatedEvent.Trim(), cancellationToken);
                eventId = eventObj?.EventId;
            }

            var ticket = new SupportTicket
            {
                TicketId = Guid.NewGuid(),
                TicketNo = ticketNo,
                UserId = userId,
                EventId = eventId,
                TicketTypeId = ticketTypeId,
                PriorityId = priorityId,
                StatusId = statusId,
                MemberName = request.MemberName.Trim(),
                MemberId = request.MemberId?.Trim(),
                RelatedEvent = request.RelatedEvent?.Trim(),
                TicketType = ticketType ?? string.Empty,
                Subject = string.Empty,
                Description = request.Description.Trim(),
                Status = status,
                Priority = priority,
                AssignedTo = null,
                RefNo = refNo,
                Utr = request.Utr?.Trim(),
                Attachment = request.Attachment?.Trim(),
                IsActive = true,
                IsDeleted = false,
                CreatedBy = CommonMethods.ParseNullableGuid(user),
                CreatedAt = DateTime.UtcNow
            };

            await _ticketRepository.AddAsync(ticket, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(CommonLogMessages.SupportTickets.TicketCreated, ticketNo, request.MemberName);

            // Safe non-blocking email notification dispatch to active organizers
            await SendSupportTicketEmailNotificationAsync(ticket, cancellationToken);

            return _mapper.Map<SupportTicketDto>(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(CreateAsync));
            throw;
        }
    }

    public async Task<SupportTicketDto> UpdateAsync(Guid ticketId, UpdateSupportTicketRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.SupportTickets.NotFound);

            if (!string.IsNullOrWhiteSpace(request.MemberName))
            {
                ticket.MemberName = request.MemberName.Trim();
                if (_memberRepository != null)
                {
                    var userObj = await _memberRepository.GetUserByNameAsync(ticket.MemberName, cancellationToken);
                    if (userObj != null) ticket.UserId = userObj.UserId;
                }
            }

            if (request.MemberId != null)
            {
                ticket.MemberId = string.IsNullOrWhiteSpace(request.MemberId) ? null : request.MemberId.Trim();
                if (Guid.TryParse(ticket.MemberId, out var parsedGuid))
                {
                    ticket.UserId = parsedGuid;
                }
            }

            if (request.RelatedEvent != null)
            {
                ticket.RelatedEvent = string.IsNullOrWhiteSpace(request.RelatedEvent) ? null : request.RelatedEvent.Trim();
                if (!string.IsNullOrWhiteSpace(ticket.RelatedEvent) && _eventRepository != null)
                {
                    var eventObj = await _eventRepository.GetByNameAsync(ticket.RelatedEvent, cancellationToken);
                    ticket.EventId = eventObj?.EventId;
                }
                else if (string.IsNullOrWhiteSpace(ticket.RelatedEvent))
                {
                    ticket.EventId = null;
                }
            }

            if (!string.IsNullOrWhiteSpace(request.TicketType))
            {
                ticket.TicketType = request.TicketType.Trim();
                if (_ticketTypeRepository != null)
                {
                    var typeObj = await _ticketTypeRepository.GetByNameAsync(ticket.TicketType, cancellationToken);
                    if (typeObj != null) ticket.TicketTypeId = typeObj.TicketTypeId;
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Subject))
            {
                ticket.Subject = request.Subject.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Description))
            {
                ticket.Description = request.Description.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                ticket.Status = request.Status.Trim();
                if (_statusRepository != null)
                {
                    var statusObj = await _statusRepository.GetByNameAsync(ticket.Status, cancellationToken);
                    if (statusObj != null) ticket.StatusId = statusObj.StatusId;
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Priority))
            {
                ticket.Priority = request.Priority.Trim();
                if (_priorityRepository != null)
                {
                    var priorityObj = await _priorityRepository.GetByNameAsync(ticket.Priority, cancellationToken);
                    if (priorityObj != null) ticket.PriorityId = priorityObj.PriorityId;
                }
            }

            if (request.RefNo != null)
            {
                ticket.RefNo = string.IsNullOrWhiteSpace(request.RefNo) ? null : request.RefNo.Trim();
            }

            if (request.Utr != null)
            {
                ticket.Utr = string.IsNullOrWhiteSpace(request.Utr) ? null : request.Utr.Trim();
            }

            if (request.Attachment != null)
            {
                ticket.Attachment = string.IsNullOrWhiteSpace(request.Attachment) ? null : request.Attachment.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.ResolutionNotes))
            {
                ticket.ResolutionNotes = request.ResolutionNotes.Trim();
            }

            ticket.ModifiedBy = CommonMethods.ParseNullableGuid(user);
            ticket.ModifiedOn = DateTime.UtcNow;

            _ticketRepository.Update(ticket);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(CommonLogMessages.SupportTickets.TicketUpdated, ticket.TicketNo, ticket.Status);

            return _mapper.Map<SupportTicketDto>(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(UpdateAsync));
            throw;
        }
    }

    public async Task<SupportTicketDto> ReplyAsync(Guid ticketId, ReplyTicketRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.SupportTickets.NotFound);

            var timestamp = DateTime.UtcNow.ToString("dd MMM yyyy, hh:mm tt");
            var sender = string.IsNullOrWhiteSpace(user) ? (string.IsNullOrWhiteSpace(ticket.MemberName) ? "User" : ticket.MemberName) : user.Trim();
            var newNote = $"[{timestamp}] {sender}: {request.Message.Trim()}";

            if (string.IsNullOrWhiteSpace(ticket.ResolutionNotes))
            {
                ticket.ResolutionNotes = newNote;
            }
            else
            {
                ticket.ResolutionNotes += "\n            " + newNote;
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                ticket.Status = request.Status.Trim();
                if (_statusRepository != null)
                {
                    var statusObj = await _statusRepository.GetByNameAsync(ticket.Status, cancellationToken);
                    if (statusObj != null) ticket.StatusId = statusObj.StatusId;
                }
            }

            ticket.ModifiedBy = CommonMethods.ParseNullableGuid(user);
            ticket.ModifiedOn = DateTime.UtcNow;

            _ticketRepository.Update(ticket);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<SupportTicketDto>(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(ReplyAsync));
            throw;
        }
    }

    public async Task DeleteAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        try
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.SupportTickets.NotFound);

            _ticketRepository.Delete(ticket);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(CommonLogMessages.SupportTickets.TicketDeleted, ticketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(DeleteAsync));
            throw;
        }
    }

    private async Task SendSupportTicketEmailNotificationAsync(SupportTicket ticket, CancellationToken cancellationToken)
    {
        try
        {
            if (_emailService == null)
            {
                _logger.LogWarning("IEmailService is not available. Skipping email notification for support ticket {TicketNo}.", ticket.TicketNo);
                return;
            }

            var organizerEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Resolve Event-Specific Organizer if ticket is associated with an event
            if (ticket.EventId.HasValue && _eventRepository != null)
            {
                try
                {
                    var eventObj = await _eventRepository.GetByIdAsync(ticket.EventId.Value, cancellationToken);
                    if (eventObj != null && eventObj.CreatedBy != Guid.Empty && _userRepository != null)
                    {
                        var creatorUser = await _userRepository.GetByIdAsync(eventObj.CreatedBy, cancellationToken);
                        if (creatorUser != null && creatorUser.IsActive && !creatorUser.IsDeleted && !string.IsNullOrWhiteSpace(creatorUser.Email))
                        {
                            organizerEmails.Add(creatorUser.Email.Trim());
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not resolve event organizer for EventId {EventId}.", ticket.EventId);
                }
            }

            // 2. Resolve Global Active Organizers & Admins (PrimaryRole/SecondaryRole == Organizer or Admin)
            if (_userRepository != null)
            {
                try
                {
                    Guid? organizerRoleId = null;
                    Guid? adminRoleId = null;
                    if (_roleRepository != null)
                    {
                        var organizerRole = await _roleRepository.GetByNameAsync(CommonConstants.UserRoles.Organizer, cancellationToken);
                        organizerRoleId = organizerRole?.RoleId;
                        var adminRole = await _roleRepository.GetByNameAsync(CommonConstants.UserRoles.Admin, cancellationToken);
                        adminRoleId = adminRole?.RoleId;
                    }

                    var allUsers = await _userRepository.GetAllAsync(cancellationToken);
                    foreach (var u in allUsers)
                    {
                        if (u.IsActive && !u.IsDeleted && !string.IsNullOrWhiteSpace(u.Email))
                        {
                            bool isOrganizerOrAdmin =
                                (organizerRoleId.HasValue && (u.RoleIds.Contains(organizerRoleId.Value) || u.PrimaryRoleIds.Contains(organizerRoleId.Value) || u.SecondaryRoleIds.Contains(organizerRoleId.Value)))
                                || (adminRoleId.HasValue && (u.RoleIds.Contains(adminRoleId.Value) || u.PrimaryRoleIds.Contains(adminRoleId.Value) || u.SecondaryRoleIds.Contains(adminRoleId.Value)))
                                || (u.RoleId.HasValue && ((organizerRoleId.HasValue && u.RoleId.Value == organizerRoleId.Value) || (adminRoleId.HasValue && u.RoleId.Value == adminRoleId.Value)))
                                || string.Equals(u.Role, CommonConstants.UserRoles.Admin, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(u.Role, CommonConstants.UserRoles.Organizer, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(u.RoleName, CommonConstants.UserRoles.Admin, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(u.RoleName, CommonConstants.UserRoles.Organizer, StringComparison.OrdinalIgnoreCase);

                            if (isOrganizerOrAdmin)
                            {
                                organizerEmails.Add(u.Email.Trim());
                            }
                        }
                    }
                    if (organizerEmails.Count == 0 && allUsers.Count > 0)
                    {
                        foreach (var u in allUsers)
                        {
                            if (u.IsActive && !u.IsDeleted && !string.IsNullOrWhiteSpace(u.Email))
                            {
                                organizerEmails.Add(u.Email.Trim());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not query global active organizers/admins for support ticket email notification.");
                }
            }

            if (organizerEmails.Count == 0)
            {
                _logger.LogWarning("No active Organizer found for support ticket {TicketNo} (RefNo: {RefNo}, EventId: {EventId}).", ticket.TicketNo, ticket.RefNo, ticket.EventId);
                return;
            }

            // 3. Resolve Member Contact Info & add to recipient list
            string? memberEmail = null;
            string? memberPhone = null;
            if (ticket.UserId.HasValue && _userRepository != null)
            {
                try
                {
                    var userObj = await _userRepository.GetByIdAsync(ticket.UserId.Value, cancellationToken);
                    if (userObj != null)
                    {
                        memberEmail = userObj.Email;
                        memberPhone = userObj.Phone;
                    }
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(memberEmail) && _currentUserService != null)
            {
                memberEmail = _currentUserService.Email;
            }

            if (!string.IsNullOrWhiteSpace(memberEmail))
            {
                organizerEmails.Add(memberEmail.Trim());
            }

            // 4. Build Email Subject and Body
            var subjectTitle = !string.IsNullOrWhiteSpace(ticket.Subject) ? ticket.Subject : (!string.IsNullOrWhiteSpace(ticket.TicketType) ? ticket.TicketType : "Support Request");
            var subject = $"[Support Ticket #{ticket.TicketNo}] New Support Request: {subjectTitle}";
            var htmlBody = BuildSupportTicketEmailHtml(ticket, memberEmail, memberPhone);

            // 5. Send Email to Recipients safely
            foreach (var recipientEmail in organizerEmails)
            {
                try
                {
                    await _emailService.SendEmailAsync(recipientEmail, subject, htmlBody, cancellationToken: cancellationToken);
                    _logger.LogInformation("Support ticket email notification sent successfully. TicketNo: {TicketNo}, Recipient: {RecipientEmail}", ticket.TicketNo, recipientEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send support ticket email notification. TicketNo: {TicketNo}, Recipient: {RecipientEmail}", ticket.TicketNo, recipientEmail);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process support ticket email notification. TicketNo: {TicketNo}", ticket.TicketNo);
        }
    }

    private string BuildSupportTicketEmailHtml(SupportTicket ticket, string? memberEmail, string? memberPhone)
    {
        var frontendBaseUrl = _configuration?["Cors:AllowedOrigins:0"]
            ?? _configuration?["FrontendUrl"]
            ?? CommonConstants.Defaults.DefaultFrontendUrl;

        var viewTicketUrl = $"{frontendBaseUrl.TrimEnd('/')}/support-tickets";

        var priorityLower = (ticket.Priority ?? "medium").ToLowerInvariant();
        var badgeClass = priorityLower.Contains("urgent") ? "urgent" : (priorityLower.Contains("high") ? "high" : (priorityLower.Contains("low") ? "low" : "medium"));

        var relatedEventRow = !string.IsNullOrWhiteSpace(ticket.RelatedEvent)
            ? $"<tr><td class=\"label\">Related Event:</td><td class=\"value\">{WebUtility.HtmlEncode(ticket.RelatedEvent)}</td></tr>"
            : string.Empty;

        var memberEmailRow = !string.IsNullOrWhiteSpace(memberEmail)
            ? $"<tr><td class=\"label\">Email:</td><td class=\"value\">{WebUtility.HtmlEncode(memberEmail)}</td></tr>"
            : string.Empty;

        var memberPhoneRow = !string.IsNullOrWhiteSpace(memberPhone)
            ? $"<tr><td class=\"label\">Phone:</td><td class=\"value\">{WebUtility.HtmlEncode(memberPhone)}</td></tr>"
            : string.Empty;

        var createdAtText = (ticket.CreatedAt ?? DateTime.UtcNow).ToString("dd MMMM yyyy, hh:mm tt");

        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f6f9; color: #333333; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.05); }}
        .header {{ background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%); color: #ffffff; padding: 25px 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 22px; font-weight: 600; }}
        .header p {{ margin: 5px 0 0 0; font-size: 14px; opacity: 0.9; }}
        .content {{ padding: 30px; }}
        .section-title {{ font-size: 16px; font-weight: 700; color: #1e3c72; border-bottom: 2px solid #eef2f5; padding-bottom: 8px; margin-top: 20px; margin-bottom: 15px; }}
        .info-table {{ width: 100%; border-collapse: collapse; margin-bottom: 15px; }}
        .info-table td {{ padding: 8px 12px; font-size: 14px; vertical-align: top; }}
        .info-table td.label {{ font-weight: 600; color: #666666; width: 35%; background-color: #fafbfc; border-radius: 4px; }}
        .info-table td.value {{ color: #222222; }}
        .badge {{ display: inline-block; padding: 4px 10px; font-size: 12px; font-weight: 600; border-radius: 12px; }}
        .badge-urgent {{ background-color: #ffebee; color: #c62828; }}
        .badge-high {{ background-color: #fff3e0; color: #e65100; }}
        .badge-medium {{ background-color: #e3f2fd; color: #1565c0; }}
        .badge-low {{ background-color: #f1f8e9; color: #33691e; }}
        .description-box {{ background-color: #f8f9fa; border-left: 4px solid #2a5298; padding: 12px 15px; margin-top: 5px; border-radius: 0 4px 4px 0; font-size: 14px; line-height: 1.5; }}
        .btn-container {{ text-align: center; margin-top: 30px; margin-bottom: 10px; }}
        .btn {{ display: inline-block; background-color: #2a5298; color: #ffffff !important; text-decoration: none; padding: 12px 28px; font-size: 15px; font-weight: 600; border-radius: 6px; box-shadow: 0 2px 5px rgba(0,0,0,0.15); }}
        .footer {{ background-color: #f8f9fa; text-align: center; padding: 15px; font-size: 12px; color: #888888; border-top: 1px solid #eeeeee; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>New Support Ticket Submitted</h1>
            <p>Team Contribution Management System</p>
        </div>
        <div class=""content"">
            <p style=""font-size: 15px; margin-top: 0;"">Hello Organizer,</p>
            <p style=""font-size: 14px; color: #555555;"">A new support ticket has been submitted by a member and requires your attention.</p>
            
            <div class=""section-title"">Ticket Details</div>
            <table class=""info-table"">
                <tr>
                    <td class=""label"">Ticket No:</td>
                    <td class=""value""><strong>{WebUtility.HtmlEncode(ticket.TicketNo)}</strong></td>
                </tr>
                <tr>
                    <td class=""label"">Reference No:</td>
                    <td class=""value"">{WebUtility.HtmlEncode(ticket.RefNo ?? string.Empty)}</td>
                </tr>
                <tr>
                    <td class=""label"">Ticket Type:</td>
                    <td class=""value"">{WebUtility.HtmlEncode(ticket.TicketType)}</td>
                </tr>
                <tr>
                    <td class=""label"">Priority:</td>
                    <td class=""value""><span class=""badge badge-{badgeClass}"">{WebUtility.HtmlEncode(ticket.Priority)}</span></td>
                </tr>
                {relatedEventRow}
                <tr>
                    <td class=""label"">Created At:</td>
                    <td class=""value"">{createdAtText}</td>
                </tr>
            </table>

            <div class=""section-title"">Description</div>
            <div class=""description-box"">{WebUtility.HtmlEncode(ticket.Description)}</div>

            <div class=""section-title"">Member Details</div>
            <table class=""info-table"">
                <tr>
                    <td class=""label"">Name:</td>
                    <td class=""value""><strong>{WebUtility.HtmlEncode(ticket.MemberName)}</strong></td>
                </tr>
                {memberEmailRow}
                {memberPhoneRow}
            </table>

            <div class=""btn-container"">
                <a href=""{viewTicketUrl}"" class=""btn"" target=""_blank"">View Support Ticket</a>
            </div>
        </div>
        <div class=""footer"">
            This is an automated notification from Team Contribution Management System.
        </div>
    </div>
</body>
</html>";
    }
}

