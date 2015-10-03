for %%1 in (
    Microsoft.Extensions.CodeGeneration.Core.FunctionalTest
    Microsoft.Extensions.CodeGeneration.Core.Test
    Microsoft.Extensions.CodeGeneration.Templating.Test
    Microsoft.Extensions.CodeGeneration.EntityFramework.Test
    ) do (
        cd %%1
        dnx test
        cd ..
    )