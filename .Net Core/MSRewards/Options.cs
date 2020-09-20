using CommandLine;

namespace MSRewards
{
    class Options
    {
        [Option('F', "use-firefox", Required = false, HelpText = "Use Firefox")]
        public bool Firefox { get; set; }


        [Option('E',"email", Required = true, HelpText = "Email ID")]
        public string Email { get; set; }

        [Option('P', "password", Required = true, HelpText = "password")]
        public string Password { get; set; }

    }
}
