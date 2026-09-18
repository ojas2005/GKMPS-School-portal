using SchoolERP.Business.Identity.Passwords;

namespace SchoolERP.Tests;

public class PasswordPolicyTests
{
    [Theory]
    [InlineData("12345678")]           // too short
    [InlineData("password")]
    [InlineData("aaaaaaaaaaaa")]       // one repeated character
    [InlineData("1234567890")]         // number run
    [InlineData("0987654321")]
    [InlineData("qwertyuiop")]         // keyboard run
    [InlineData("Password@123")]       // a common word dressed up
    [InlineData("P@ssw0rd2026!")]
    [InlineData("Welcome@2024")]
    [InlineData("Student@12345")]
    [InlineData("School#2026!!")]
    [InlineData("iloveyou1234")]
    public void Guessable_passwords_are_refused(string password)
    {
        Assert.NotEmpty(PasswordPolicy.Check(password));
    }

    [Theory]
    [InlineData("OwnerGKMPS@2005", "OjasIsOwner", "GKMPS School")]   // the school's name
    [InlineData("meera.sharma2026", "meera.sharma", null)]            // the login ID
    [InlineData("Sharma family 1990", "msharma", "Meera Sharma")]     // their own name
    public void Passwords_built_from_personal_words_are_refused(string password, string? login, string? name)
    {
        Assert.NotEmpty(PasswordPolicy.Check(password, login, name));
    }

    [Theory]
    [InlineData("correct horse battery staple")]
    [InlineData("mango-river-lantern")]
    [InlineData("Tuesday!Kettle9Orbit")]
    [InlineData("my own passphrase 2026")]
    [InlineData("Review@12345")]
    public void Long_unguessable_passwords_are_accepted(string password)
    {
        Assert.Empty(PasswordPolicy.Check(password, "teacher10a", "Meera Sharma", "GKMPS School"));
    }
}
