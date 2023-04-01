using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using OpenAI.GPT3;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels.ResponseModels;
using System.Reflection;

class Program
{
    // This example requires environment variables named "SPEECH_KEY" and "SPEECH_REGION"
    static string speechKey = Environment.GetEnvironmentVariable("SPEECH_KEY");
    static string speechRegion = Environment.GetEnvironmentVariable("SPEECH_REGION");
    static string openaiapikey = Environment.GetEnvironmentVariable("OPENAI_APIKEY");

    static void OutputSpeechSynthesisResult(SpeechSynthesisResult speechSynthesisResult, string text)
    {
        switch (speechSynthesisResult.Reason)
        {
            case ResultReason.SynthesizingAudioCompleted:
                Console.WriteLine($"Speech synthesized for text: [{text}]");
                break;
            case ResultReason.Canceled:
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(speechSynthesisResult);
                Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                    Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                }
                break;
            default:
                break;
        }
    }

    static void OutputSpeechRecognitionResult(SpeechRecognitionResult speechRecognitionResult)
    {
        switch (speechRecognitionResult.Reason)
        {
            case ResultReason.RecognizedSpeech:
                Console.WriteLine($"RECOGNIZED: Text={speechRecognitionResult.Text}");
                break;
            case ResultReason.NoMatch:
                Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                break;
            case ResultReason.Canceled:
                var cancellation = CancellationDetails.FromResult(speechRecognitionResult);
                Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                    Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                }
                break;
        }
    }

    async static Task Main(string[] args)
    {
        while (true)
        {
            OpenAIService openAiService = SetupOpenAI();
            SpeechConfig speechConfig = SetupSpeechService();

            Console.WriteLine("Speak into your microphone.");
            using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

            var speechRecognitionResult = await speechRecognizer.RecognizeOnceAsync();
            OutputSpeechRecognitionResult(speechRecognitionResult);

            Console.WriteLine("Asking question...");
            string question = speechRecognitionResult.Text;
            ChatCompletionCreateResponse completionResult = await GetCompletionResult(openAiService, question);

            await ProcessCompletionResult(speechConfig, completionResult);
        }
    }

    private static async Task<ChatCompletionCreateResponse> GetCompletionResult(OpenAIService openAiService, string question)
    {
        return await openAiService.ChatCompletion.CreateCompletion(new OpenAI.GPT3.ObjectModels.RequestModels.ChatCompletionCreateRequest
        {
            Messages = new List<ChatMessage>
            {
                ChatMessage.FromUser(question)
            },
            Model = Models.ChatGpt3_5Turbo

        });
    }

    private static async Task ProcessCompletionResult(SpeechConfig speechConfig, ChatCompletionCreateResponse completionResult)
    {
        if (completionResult.Successful)
        {
            using (var speechSynthesizer = new SpeechSynthesizer(speechConfig))
            {
                string content = completionResult.Choices.First().Message.Content;
                var speechSynthesisResult = await speechSynthesizer.SpeakTextAsync(content);
                OutputSpeechSynthesisResult(speechSynthesisResult, content);
            }
        }
    }

    private static SpeechConfig SetupSpeechService()
    {
        var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);

        speechConfig.SpeechSynthesisVoiceName = "en-US-JennyNeural";
        speechConfig.SpeechRecognitionLanguage = "en-US";

        return speechConfig;
    }

    private static OpenAIService SetupOpenAI()
    {
        var openAiService = new OpenAIService(new OpenAiOptions()
        {
            ApiKey = openaiapikey
        });

        openAiService.SetDefaultModelId(Models.Davinci);
        return openAiService;
    }

}