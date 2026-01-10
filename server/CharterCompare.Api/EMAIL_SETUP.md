# Email Configuration Guide

This guide explains how to set up email sending for development and testing.

## Development Setup

### Option 1: Gmail (Recommended for Quick Testing)

1. **Enable App Passwords in Gmail:**
   - Go to your Google Account settings
   - Enable 2-Step Verification
   - Go to "App passwords" and generate a new app password for "Mail"
   - Copy the 16-character password

2. **Update `appsettings.Development.json`:**
   ```json
   {
     "Email": {
       "Smtp": {
         "Host": "smtp.gmail.com",
         "Port": "587",
         "Username": "your-email@gmail.com",
         "Password": "your-app-password",
         "EnableSsl": "true"
       },
       "From": {
         "Address": "your-email@gmail.com",
         "Name": "CharterCompare"
       }
     }
   }
   ```

### Option 2: Mailtrap (Recommended for Development)

Mailtrap is a service that catches emails in development without sending real emails.

1. **Sign up at [mailtrap.io](https://mailtrap.io)** (free tier available)
2. **Get your SMTP credentials** from the inbox settings
3. **Update `appsettings.Development.json`:**
   ```json
   {
     "Email": {
       "Smtp": {
         "Host": "smtp.mailtrap.io",
         "Port": "2525",
         "Username": "your-mailtrap-username",
         "Password": "your-mailtrap-password",
         "EnableSsl": "false"
       },
       "From": {
         "Address": "noreply@chartercompare.com",
         "Name": "CharterCompare"
       }
     }
   }
   ```

### Option 3: Local SMTP Server

If you have a local SMTP server (like MailHog or Papercut), configure it:

```json
{
  "Email": {
    "Smtp": {
      "Host": "localhost",
      "Port": "1025",
      "Username": "",
      "Password": "",
      "EnableSsl": "false"
    },
    "From": {
      "Address": "test@localhost",
      "Name": "CharterCompare"
    }
  }
}
```

## Testing

1. **Start the backend API**
2. **Submit a request through the chat interface**
3. **Check your email inbox** (or Mailtrap inbox if using Mailtrap)
4. **Click the link in the email** to verify it works

## Troubleshooting

- **Emails not sending**: Check the logs for error messages
- **Gmail blocking**: Make sure you're using an App Password, not your regular password
- **Connection timeout**: Check firewall settings and SMTP port
- **Authentication failed**: Verify username and password are correct

## Production Setup

For production, use a proper email service like:
- **SendGrid** (recommended)
- **Amazon SES**
- **Mailgun**
- **Postmark**

Update the SMTP settings accordingly in your production configuration.
