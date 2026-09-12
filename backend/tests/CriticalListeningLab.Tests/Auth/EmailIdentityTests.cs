using System.Security.Claims;
using CriticalListeningLab.Api.Auth;

namespace CriticalListeningLab.Tests.Auth;

public class EmailIdentityTests
{
    [Theory]
    [InlineData("preferred_username", "Ana.Horvat@Student.Algebra.HR", "ana.horvat@student.algebra.hr")]
    [InlineData(ClaimTypes.Email, "nastavnik@algebra.hr", "nastavnik@algebra.hr")]
    [InlineData("email", "x@algebra.hr", "x@algebra.hr")]
    [InlineData(ClaimTypes.Upn, "y@student.algebra.hr", "y@student.algebra.hr")]
    public void ReadEmail_picks_first_claim_that_looks_like_email_and_normalizes(
        string claimType, string value, string expected)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(claimType, value)
        ]));

        EmailIdentity.ReadEmail(principal).ShouldBe(expected);
    }

    [Fact]
    public void ReadEmail_skips_claims_without_at_sign()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("preferred_username", "nije-email"),
            new Claim("email", "stvarni@student.algebra.hr")
        ]));

        EmailIdentity.ReadEmail(principal).ShouldBe("stvarni@student.algebra.hr");
    }

    [Fact]
    public void ReadEmail_returns_null_when_no_email_claim_exists()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("oid", "abc")
        ]));

        EmailIdentity.ReadEmail(principal).ShouldBeNull();
    }

    [Theory]
    [InlineData("ana@student.algebra.hr", true)]
    [InlineData("nastavnik@algebra.hr", true)]
    [InlineData("ANA@ALGEBRA.HR", true)]
    [InlineData("gost@algebra.hr.napadac.com", false)]
    [InlineData("netko@gmail.com", false)]
    [InlineData("bez-et", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsAllowedDomain_compares_full_domain_exactly(string? email, bool expected)
    {
        string[] allowed = ["algebra.hr", "student.algebra.hr"];

        EmailIdentity.IsAllowedDomain(email, allowed).ShouldBe(expected);
    }

    [Fact]
    public void IsAllowedDomain_rejects_everything_when_allow_list_is_empty()
    {
        EmailIdentity.IsAllowedDomain("nastavnik@algebra.hr", []).ShouldBeFalse();
    }
}
