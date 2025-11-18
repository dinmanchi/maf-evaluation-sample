# Contributing to MAF Evaluation Sample

Thank you for your interest in contributing to this project! This document provides guidelines for contributing.

## How to Contribute

### Reporting Issues

If you find a bug or have a suggestion:

1. Check if the issue already exists in [GitHub Issues](https://github.com/dinmanchi/maf-evaluation-sample/issues)
2. If not, create a new issue with:
   - Clear, descriptive title
   - Detailed description of the problem or suggestion
   - Steps to reproduce (for bugs)
   - Expected vs actual behavior
   - Environment details (.NET version, OS, etc.)

### Submitting Changes

1. **Fork the repository**
   ```bash
   gh repo fork dinmanchi/maf-evaluation-sample --clone
   ```

2. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Make your changes**
   - Follow existing code style and conventions
   - Add comments for complex logic
   - Update documentation if needed

4. **Test your changes**
   ```bash
   dotnet build
   dotnet run
   ```

5. **Commit with clear messages**
   ```bash
   git commit -m "feat: add new evaluation metric for response relevance"
   ```
   
   Use conventional commit format:
   - `feat:` - New features
   - `fix:` - Bug fixes
   - `docs:` - Documentation changes
   - `refactor:` - Code refactoring
   - `test:` - Adding tests
   - `chore:` - Maintenance tasks

6. **Push to your fork**
   ```bash
   git push origin feature/your-feature-name
   ```

7. **Create a Pull Request**
   - Provide clear description of changes
   - Reference any related issues
   - Ensure CI checks pass

## Development Guidelines

### Code Style

- Follow C# coding conventions
- Use meaningful variable and method names
- Keep methods focused and single-purpose
- Add XML documentation comments for public APIs

### Documentation

When adding features:
- Update README.md if user-facing
- Add or update docs in `/docs` folder
- Include code examples where helpful
- Document any new environment variables

### Evaluation Metrics

When adding new evaluators:
1. Create method in `EvaluationService.cs`
2. Use structured prompts with clear scoring criteria
3. Return scores 1-5 with detailed reasoning
4. Include pass/fail threshold
5. Document the metric purpose and usage

### Testing

- Test with different queries and scenarios
- Verify evaluation scores are reasonable
- Check edge cases (empty responses, errors, etc.)
- Validate JSON output format

## Areas for Contribution

### High Priority
- Additional evaluation metrics (e.g., Safety, Groundedness)
- Support for multi-turn conversations
- Batch evaluation from JSONL datasets
- Full Azure AI Foundry cloud evaluation implementation

### Medium Priority
- Custom evaluator configuration via appsettings.json
- Evaluation result visualization
- Export results to different formats (CSV, Excel)
- Performance benchmarking

### Documentation
- Additional code examples
- Tutorial videos or blog posts
- Comparison with other evaluation approaches
- Best practices guide

## Questions?

Feel free to open an issue for any questions about contributing!

## Code of Conduct

- Be respectful and constructive
- Focus on the code, not the person
- Help others learn and grow
- Keep discussions relevant and professional

## License

By contributing, you agree that your contributions will be licensed under the same terms as the project.
