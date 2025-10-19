using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Tubeshade.Server.V1;

public static class MultipartRequestHelper
{
    // https://datatracker.ietf.org/doc/html/rfc2046#section-5.1.1
    private const int MaxBoundaryLength = 70;

    public static string GetBoundary(this MediaTypeHeaderValue contentType)
    {
        var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;
        if (string.IsNullOrWhiteSpace(boundary))
        {
            throw new InvalidDataException("Missing content-type boundary");
        }

        if (boundary.Length > MaxBoundaryLength)
        {
            throw new InvalidDataException($"Multipart boundary length limit {MaxBoundaryLength} exceeded");
        }

        return boundary;
    }

    public static bool IsMultipart(this HttpRequest request)
    {
        return request.HasFormContentType && request.ContentType.IsMultipartContentType();
    }

    private static bool IsMultipartContentType(this string? contentType) =>
        !string.IsNullOrEmpty(contentType) &&
        contentType.Contains("multipart/", StringComparison.OrdinalIgnoreCase);
}
