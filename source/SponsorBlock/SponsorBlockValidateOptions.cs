using Microsoft.Extensions.Options;

namespace SponsorBlock;

[OptionsValidator]
public sealed partial class SponsorBlockValidateOptions : IValidateOptions<SponsorBlockOptions>;
