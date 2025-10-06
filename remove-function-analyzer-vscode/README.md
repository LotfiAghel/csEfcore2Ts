# Remove Function Analyzer VS Code Extension

This VS Code extension provides a context menu command to remove a function by right-clicking on its name in a C# file.

## Setup

1. Open a terminal in this directory.

2. Run `npm install` to install dependencies.

3. Run `npm run compile` to build the extension.

4. Press `F5` in VS Code to launch a new Extension Development Host window with the extension loaded.

## Usage

- Open a C# file.
- Select the function name you want to remove.
- Right-click and choose **Remove Function** from the context menu.
- (Currently, this shows an information message. Integration with the Roslyn analyzer to actually remove the function is pending.)

## Next Steps

- Integrate with the Roslyn analyzer to invoke the refactoring that removes the selected function.
- Improve detection of the function under the cursor if no selection is made.