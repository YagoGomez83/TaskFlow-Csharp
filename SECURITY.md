# Security Policy - TaskManagement API

## Reporting a Vulnerability

We take the security of TaskManagement API seriously. If you discover a security vulnerability, please follow the responsible disclosure process outlined below.

### How to Report

**DO NOT** create a public GitHub issue for security vulnerabilities.

Instead, please report security vulnerabilities by emailing:

**security@taskmanagement.com** (or your organization's security email)

### What to Include

Please provide the following information in your report:

- **Description:** A clear description of the vulnerability
- **Impact:** The potential impact and severity
- **Steps to Reproduce:** Detailed steps to reproduce the vulnerability
- **Proof of Concept:** Code, screenshots, or video demonstrating the issue
- **Suggested Fix:** If you have one, we appreciate it
- **Disclosure Timeline:** Your preferred timeline for public disclosure

### Example Report Format

```
Subject: [SECURITY] SQL Injection in TasksController

Description:
The GetTasks endpoint is vulnerable to SQL injection through the searchTerm parameter.

Impact:
An attacker could extract sensitive data from the database or execute arbitrary SQL commands.

Severity: CRITICAL

Steps to Reproduce:
1. Send GET request to /api/tasks?searchTerm=' OR '1'='1
2. Observe SQL error revealing database structure
3. Exploit further with UNION-based injection

Proof of Concept:
GET /api/tasks?searchTerm=test' UNION SELECT username, password FROM users--

Suggested Fix:
Use parameterized queries or ORM (EF Core) instead of raw SQL concatenation.

CVE ID (if applicable): Pending

Reporter: John Doe (john.doe@example.com)
```

---

## Response Timeline

We are committed to responding promptly to security reports:

- **Initial Response:** Within 48 hours of report
- **Vulnerability Confirmed:** Within 7 days
- **Fix Released:** Within 30 days for critical issues, 90 days for others
- **Public Disclosure:** Coordinated with reporter after fix is released

---

## Vulnerability Severity Classification

We follow the CVSS v3.1 scoring system:

| Severity | CVSS Score | Response Time | Examples |
|----------|------------|---------------|----------|
| **Critical** | 9.0-10.0 | 24-48 hours | RCE, SQL Injection, Auth bypass |
| **High** | 7.0-8.9 | 7 days | XSS, CSRF, Privilege escalation |
| **Medium** | 4.0-6.9 | 30 days | Information disclosure, DoS |
| **Low** | 0.1-3.9 | 90 days | Minor information leaks |

---

## Supported Versions

We provide security updates for the following versions:

| Version | Supported          | End of Life |
|---------|--------------------|-------------|
| 1.x.x   | :white_check_mark: | TBD         |
| 0.x.x   | :x:                | 2025-01-01  |

---

## Security Measures Implemented

### Authentication & Authorization

- ✅ **JWT with Refresh Token Rotation:** Mitigates token theft
- ✅ **BCrypt Password Hashing:** Cost factor 12
- ✅ **Account Lockout:** 5 failed attempts = 15 min lockout
- ✅ **Role-Based Access Control (RBAC):** Admin and User roles
- ✅ **Claims-Based Authorization:** Fine-grained permissions

### Input Validation

- ✅ **FluentValidation:** All user inputs validated
- ✅ **XSS Prevention:** HTML encoding and sanitization
- ✅ **SQL Injection Prevention:** ORM with parameterized queries
- ✅ **CSRF Protection:** (For future forms/cookies)

### Security Headers

- ✅ **HSTS:** `Strict-Transport-Security` with 1-year max-age
- ✅ **CSP:** Content Security Policy restrictive
- ✅ **X-Frame-Options:** DENY (clickjacking prevention)
- ✅ **X-Content-Type-Options:** nosniff
- ✅ **Referrer-Policy:** strict-origin-when-cross-origin

### Rate Limiting

- ✅ **General Endpoints:** 100 requests/minute
- ✅ **Auth Endpoints:** 10 requests/minute
- ✅ **IP-based & User-based:** Dual protection

### Secrets Management

- ✅ **Development:** dotnet user-secrets
- ✅ **Production:** Environment variables
- ✅ **No Secrets in Code:** SAST verification

### Logging & Monitoring

- ✅ **Structured Logging:** Serilog with JSON format
- ✅ **Security Event Logging:** Login attempts, access denied, etc.
- ✅ **No Sensitive Data in Logs:** Passwords and tokens excluded

### Dependencies

- ✅ **Dependency Scanning:** Automated with Snyk/Trivy
- ✅ **Regular Updates:** Dependabot configured
- ✅ **Vulnerability Monitoring:** Weekly scans

### Infrastructure Security

- ✅ **Docker Security:** Non-root user, Alpine base images
- ✅ **Network Isolation:** Separate backend/frontend networks
- ✅ **HTTPS Enforcement:** Redirect HTTP → HTTPS
- ✅ **Database Security:** Strong passwords, connection encryption

---

## OWASP Top 10 Compliance

We actively mitigate risks from the [OWASP Top 10 2021](https://owasp.org/Top10/):

| OWASP Risk | Status | Mitigations |
|------------|--------|-------------|
| A01: Broken Access Control | ✅ Mitigated | RBAC, ownership validation, JWT |
| A02: Cryptographic Failures | ✅ Mitigated | BCrypt, HTTPS, secrets management |
| A03: Injection | ✅ Mitigated | EF Core ORM, input validation |
| A04: Insecure Design | ✅ Mitigated | Security by design, threat modeling |
| A05: Security Misconfiguration | ✅ Mitigated | Security headers, CORS, error handling |
| A06: Vulnerable Components | ✅ Mitigated | Dependency scanning, regular updates |
| A07: Identification Failures | ✅ Mitigated | JWT, refresh token rotation, lockout |
| A08: Software Integrity Failures | ✅ Mitigated | Docker image scanning, code signing (future) |
| A09: Logging Failures | ✅ Mitigated | Structured logging, security event tracking |
| A10: SSRF | ✅ Mitigated | Whitelist of allowed external hosts |

See [Security Design Documentation](docs/architecture/security-design.md) for detailed mitigations.

---

## Security Testing

### Automated Testing

- **SAST (Static Analysis):** SonarQube in CI/CD pipeline
- **Dependency Scanning:** Snyk/Trivy weekly
- **Unit Tests:** Security-specific test cases
- **Integration Tests:** Auth and authorization flows

### Manual Testing

- **Penetration Testing:** Recommended quarterly with OWASP ZAP
- **Code Review:** Security-focused reviews for critical features
- **Threat Modeling:** Performed during design phase

---

## Known Security Limitations

We believe in transparency. Current known limitations:

1. **MFA Not Implemented:** Planned for v1.1
2. **API Rate Limiting Per-User:** Currently IP-based, user-based planned
3. **CAPTCHA:** Not implemented for registration/login (planned)
4. **Web Application Firewall (WAF):** Recommended for production deployment
5. **DDoS Protection:** Requires infrastructure-level solution (Cloudflare, AWS Shield)

---

## Security Best Practices for Users

If you are deploying TaskManagement API:

### Development
- ✅ Use `dotnet user-secrets` for local development
- ✅ Never commit `.env` files or secrets to version control
- ✅ Keep dependencies up to date: `dotnet list package --vulnerable`

### Production
- ✅ Use strong, unique passwords for database and services
- ✅ Enable HTTPS only, disable HTTP endpoint
- ✅ Configure firewall rules to restrict access
- ✅ Use secrets management service (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault)
- ✅ Enable audit logging and monitor for suspicious activity
- ✅ Regularly backup database with encryption
- ✅ Implement WAF (Web Application Firewall)
- ✅ Use DDoS protection service
- ✅ Configure alerts for critical security events

### Docker Deployment
- ✅ Scan images before deployment: `trivy image taskmanagement-api:latest`
- ✅ Use specific image tags, not `latest`
- ✅ Run containers as non-root user (already configured)
- ✅ Use read-only filesystems where possible
- ✅ Limit container resources (CPU, memory)

---

## Security Checklist for Pull Requests

All PRs touching security-sensitive code must pass this checklist:

- [ ] No secrets or credentials hardcoded
- [ ] Input validation implemented with FluentValidation
- [ ] Authorization checks in place (ownership, roles)
- [ ] No SQL injection vulnerabilities (use EF Core)
- [ ] No XSS vulnerabilities (encode outputs)
- [ ] Error messages don't reveal sensitive information
- [ ] Logging doesn't include sensitive data
- [ ] Tests cover security scenarios
- [ ] SAST scan passes without critical issues
- [ ] Reviewed by security-aware team member

---

## Incident Response Plan

In case of a security breach:

1. **Containment:**
   - Immediately isolate affected systems
   - Revoke compromised credentials
   - Block malicious IPs

2. **Assessment:**
   - Determine scope and impact
   - Identify root cause
   - Document timeline of events

3. **Remediation:**
   - Apply security patches
   - Rotate all secrets and keys
   - Update security controls

4. **Communication:**
   - Notify affected users within 72 hours
   - Publish security advisory
   - Coordinate with security researchers

5. **Post-Incident:**
   - Conduct retrospective
   - Update security measures
   - Improve detection capabilities

---

## Security Contact

For security-related questions or concerns:

- **Email:** security@taskmanagement.com
- **PGP Key:** [Link to PGP public key]
- **Response Time:** Within 48 hours

For general support: support@taskmanagement.com

---

## Hall of Fame

We appreciate security researchers who responsibly disclose vulnerabilities. Contributors will be listed here (with permission):

- *No vulnerabilities reported yet*

---

## Additional Resources

- [OWASP Top 10](https://owasp.org/Top10/)
- [OWASP API Security Top 10](https://owasp.org/www-project-api-security/)
- [CWE Top 25](https://cwe.mitre.org/top25/)
- [NIST Cybersecurity Framework](https://www.nist.gov/cyberframework)
- [Security Design Documentation](docs/architecture/security-design.md)

---

## Updates to This Policy

This security policy may be updated periodically. Check back regularly for updates.

**Last Updated:** 2025-01-09

---

**Thank you for helping keep TaskManagement API secure!**
