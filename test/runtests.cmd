for %%1 in (
    Microsoft.Framework.CodeGeneration.Core.FunctionalTest
    Microsoft.Framework.CodeGeneration.Core.Test
    Microsoft.Framework.CodeGeneration.Templating.Test
    Microsoft.Framework.CodeGeneration.EntityFramework.Test
    ) do (
        cd %%1
        k test
        cd ..
    )