import * as vscode from 'vscode';
import { execFile } from 'child_process';
import * as path from 'path';

declare const __dirname: string;

export function activate(context: vscode.ExtensionContext) {
    let disposable = vscode.commands.registerCommand('removeFunctionAnalyzer.removeFunction', async () => {
        const editor = vscode.window.activeTextEditor;
        if (!editor) {
            vscode.window.showInformationMessage('No active editor');
            return;
        }

        const selection = editor.selection;
        const selectedText = editor.document.getText(selection).trim();

        if (!selectedText) {
            vscode.window.showInformationMessage('No function name selected');
            return;
        }

        const replacementText = await vscode.window.showInputBox({
            prompt: 'Enter replacement text for function invocations',
            placeHolder: 'e.g. 0, "default", null, etc.'
        });

        if (replacementText === undefined) {
            vscode.window.showInformationMessage('Replacement text input cancelled');
            return;
        }

        const filePath = editor.document.uri.fsPath;

        // Path to the RemoveFunctionAnalyzer executable (adjust as needed)
        const analyzerExePath = path.join(__dirname, '..', '..', 'RemoveFunctionAnalyzer', 'bin', 'Debug', 'net7.0', 'RemoveFunctionAnalyzer.dll');

        // Run the RemoveFunctionAnalyzer tool as a dotnet process
        execFile('dotnet', [analyzerExePath, filePath, selectedText, replacementText], (error: any, stdout: any, stderr: any) => {
            if (error) {
                vscode.window.showErrorMessage(`Error removing function: ${error.message}`);
                return;
            }
            if (stderr) {
                vscode.window.showErrorMessage(`Error removing function: ${stderr}`);
                return;
            }

            vscode.window.showInformationMessage(stdout);

            // Reload the document to reflect changes
            vscode.workspace.openTextDocument(filePath).then((doc: vscode.TextDocument) => {
                vscode.window.showTextDocument(doc, editor.viewColumn);
            });
        });
    });

    context.subscriptions.push(disposable);
}

export function deactivate() {}