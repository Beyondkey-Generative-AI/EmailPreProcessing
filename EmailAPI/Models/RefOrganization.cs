using System;
using System.Collections.Generic;

namespace EmailManagementAPI.Models;

public partial class RefOrganization
{
    public Guid Id { get; set; }

    public string? OrgName { get; set; }

    public string? Address { get; set; }

    public DateTime? DateCreated { get; set; }

    public DateTime? DateModified { get; set; }
}
