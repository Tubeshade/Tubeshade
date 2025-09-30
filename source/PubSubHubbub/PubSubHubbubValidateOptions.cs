using Microsoft.Extensions.Options;

namespace PubSubHubbub;

[OptionsValidator]
public sealed partial class PubSubHubbubValidateOptions : IValidateOptions<PubSubHubbubOptions>;
