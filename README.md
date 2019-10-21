# dotNext 2019 Roslyn Demo

1. Рекурсивный анализ кода: [`ThrowInDisposeAnalyzer`](DotNext/DotNext/StaticAnalysis/ThrowInDispose/ThrowInDisposeAnalyzer.cs) и [`NestedInvocationWalker`](DotNext/DotNext/StaticAnalysis/NestedInvocationWalker.cs)
2. Агрегация анализаторов вокруг собственной семантической модели: [`ControllerAnalyzer`](DotNext/DotNext/StaticAnalysis/Controller/ControllerAnalyzer.cs), [`ControllerModel`](DotNext/DotNext/StaticAnalysis/Controller/ControllerModel.cs), [`ControllerActionDuplicateAnalyzer`](DotNext/DotNext/StaticAnalysis/Controller/ControllerActionDuplicateAnalyzer.cs)
3. Собственный IntelliSense для написания SQL-запросов в Dapper ORM: [`DapperCompletionProvider`](DotNext/DotNext/IntelliSense/DapperCompletionProvider.cs)
4. Использование Roslyn для навигации между сущностями в коде: [`UnitTestsNavigationProvider`](DotNext/DotNext/Refactorings/UnitTestsNavigationProvider.cs)
5. Подавление диагностик с помощью code comments и suppression file: [`WhatTheHeckAnalyzer`](DotNext/DotNext/StaticAnalysis/WhatTheHeck/WhatTheHeckAnalyzer.cs), [`SuppressionManager`](DotNext/DotNext/StaticAnalysis/SuppressionManager.cs), [`SuppressionCodeFixProvider`](DotNext/DotNext/StaticAnalysis/SuppressionCodeFixProvider.cs)

В папке [Samples](Samples) лежат тестовые solution'ы для демок.
Порядок действий такой:
1. Открыть солюшн `DotNext.sln`
2. Запустить экспериментальный instance Visual Studio, нажав Ctrl+F5
3. В нём открыть нужный солюшн с нужной демкой из папки [Samples](Samples)
