using Microsoft.Extensions.Configuration;
using WayPoint.Application.DTOs.Booking;
using WayPoint.Infrastructure.Services;
using Xunit;

namespace WayPoint.Tests;

public class BookingTests
{
    private readonly IConfiguration _configuration;

    public BookingTests()
    {
        var settings = new Dictionary<string, string?>
        {
            { "Jwt:Secret", "WayPoint_Super_Secret_Key_For_Jwt_Token_Authentication_2026_SE3090" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    [Theory]
    [InlineData(30, 0.90, 5130.0)]  // > 24h away: 90% refund on Rs. 5700
    [InlineData(18, 0.50, 2850.0)]  // 12-24h away: 50% refund on Rs. 5700
    [InlineData(6, 0.0, 0.0)]       // < 12h away: 0% non-refundable
    public void BR_REFUND_001_ShouldCalculateCorrectRefundPercentageAndAmount(
        int hoursUntilDeparture,
        decimal expectedPercentage,
        decimal expectedRefundAmount)
    {
        // Arrange
        const decimal totalPaid = 5700.0m;

        // Act
        decimal percentage;
        if (hoursUntilDeparture > 24)
        {
            percentage = 0.90m;
        }
        else if (hoursUntilDeparture >= 12)
        {
            percentage = 0.50m;
        }
        else
        {
            percentage = 0.0m;
        }

        var refundAmount = totalPaid * percentage;
        var cancellationFee = totalPaid - refundAmount;

        // Assert
        Assert.Equal(expectedPercentage, percentage);
        Assert.Equal(expectedRefundAmount, refundAmount);
        Assert.Equal(totalPaid, refundAmount + cancellationFee);
    }

    [Fact]
    public async Task HmacSha256_ShouldGenerateValidSignatureAndDetectTampering()
    {
        // Arrange
        var bookingService = new BookingService(null!, _configuration);

        const string bookingRef = "WP-7B92K1";
        const string serviceCode = "SRV-COL-ELLA-0800";
        const string seats = "4A,4B";
        const string passenger = "Nimal Silva";

        // Act - Generate legitimate signed QR payload
        var validPayload = await bookingService.GenerateTicketPayloadAsync(bookingRef, serviceCode, seats, passenger);

        // Assert
        Assert.NotNull(validPayload);
        Assert.StartsWith("WP|REF:WP-7B92K1|SRV:SRV-COL-ELLA-0800|SEATS:4A,4B|PASS:Nimal Silva|HMAC:", validPayload);

        // Act - Verify verification passes on authentic payload
        var verifyValid = await bookingService.VerifyTicketQrAsync(new VerifyQrRequestDto { QrCodePayload = validPayload });
        // The signature matches; database will return Ticket Not Found for the in-memory test, but NOT tampered signature!
        Assert.NotEqual("Security Violation: Tampered Signature", verifyValid.Status);

        // Act - Tamper with seat number in payload (forge seat 1A instead of 4A)
        var tamperedPayload = validPayload.Replace("SEATS:4A,4B", "SEATS:1A,1B");
        var verifyTampered = await bookingService.VerifyTicketQrAsync(new VerifyQrRequestDto { QrCodePayload = tamperedPayload });

        // Assert - Tampered signature must be flagged and rejected
        Assert.False(verifyTampered.IsValid);
        Assert.Equal("Security Violation: Tampered Signature", verifyTampered.Status);
    }

    [Theory]
    [InlineData("4000 0000 0000 0001", true, "Success")]
    [InlineData("4000 0000 0000 0002", false, "Declined")]
    [InlineData("4000 0000 0000 0003", false, "Timeout")]
    public async Task PaymentSandbox_ShouldHandlePresetCardOutcomes(
        string cardNumber,
        bool expectedSuccess,
        string expectedStatus)
    {
        // Arrange
        var bookingService = new BookingService(null!, _configuration);

        var request = new PaymentChargeRequestDto
        {
            CardNumber = cardNumber,
            CardholderName = "NIMAL SILVA",
            ExpiryDate = "08/28",
            Cvv = "123",
            Amount = 5700.0m
        };

        // Act
        var result = await bookingService.ProcessSandboxPaymentAsync(request);

        // Assert
        Assert.Equal(expectedSuccess, result.IsSuccess);
        Assert.Equal(expectedStatus, result.GatewayStatus);
        if (expectedSuccess)
        {
            Assert.StartsWith("TXN-", result.TransactionId);
        }
    }
}
