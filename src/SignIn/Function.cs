using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using SignIn.Contracts;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SignIn
{

    public class Function
    {
        private readonly ISignInRepository _signInRepository;
        
        private readonly ISignUpRepository _singUpRepository;

        public Function(ISignInRepository signInRepository, ISignUpRepository singUpRepository)
        {
            _signInRepository = signInRepository;
            _singUpRepository = singUpRepository;
        }

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            var requestBody = JsonSerializer.Deserialize<Dictionary<string, string>>(apigProxyEvent.Body);
            var username = requestBody["Username"];
            var password = requestBody["Password"];
            requestBody.TryGetValue("Email", out var email);

            var signInResponse = await _signInRepository.Authenticate(username, password);

            if (signInResponse.Success)
            {
                return CreateResponse(signInResponse);
            }
            
            var signUpRequest = new SignUpRequest(username, password, email);
                
            await _singUpRepository.Register(signUpRequest);
                
            signInResponse = await _signInRepository.Authenticate(username, password);

            return CreateResponse(signInResponse);
        }

        private static APIGatewayProxyResponse CreateResponse(SingInResponse authResponse)
        {
            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(authResponse),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
}
