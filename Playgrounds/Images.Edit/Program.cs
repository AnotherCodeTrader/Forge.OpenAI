﻿using Forge.OpenAI;
using Forge.OpenAI.Interfaces.Services;
using Forge.OpenAI.Models;
using Forge.OpenAI.Models.Common;
using Forge.OpenAI.Models.Images;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Images.Edit
{

    internal class Program
    {

        static async Task Main(string[] args)
        {
            using var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((builder, services) =>
            {
                services.AddForgeOpenAI(options => {
                    options.AuthenticationInfo = builder.Configuration["OpenAI:ApiKey"]!;
                });
            })
            .Build();

            IOpenAIService openAi = host.Services.GetService<IOpenAIService>()!;

            // Images should be in png format with ARGB. I got help from this website to generate sample mask
            // https://www.online-image-editor.com/

            ImageEditRequest request = new ImageEditRequest();
            request.Image = new BinaryContentData() { ContentName = "Original Image", SourceStream = File.OpenRead("image_edit_original.png") };
            
            using (request.Image.SourceStream)
            {
                request.Mask = new BinaryContentData() { ContentName = "Mask Image", SourceStream = File.OpenRead("image_edit_mask.png") };
                using (request.Mask.SourceStream)
                {
                    //request.Prompt = "A sunlit indoor lounge area with a pool containing a cat";
                    request.Prompt = "A boy rides away on a bicycle on the road";

                    HttpOperationResult<ImageEditResponse> response = await openAi.ImageService.EditImageAsync(request, CancellationToken.None).ConfigureAwait(false);
                    if (response.IsSuccess)
                    {
                        Console.WriteLine(response.Result!);

                        response.Result!.ImageData.ForEach(imageData => OpenUrl(imageData.ImageUrl));
                    }
                    else
                    {
                        Console.WriteLine(response);
                    }
                }
            }
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }

    }

}