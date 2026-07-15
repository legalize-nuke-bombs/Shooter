package com.example.shooter.auth;

import com.example.shooter.user.PasswordValidationService;
import com.example.shooter.user.UserConstants;
import jakarta.validation.constraints.*;
import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class LoginRequest {
    @NotNull
    @Size(max= UserConstants.USERNAME_MAX_LENGTH)
    private String username;

    @NotNull
    @Size(max= PasswordValidationService.PASSWORD_MAX_LENGTH)
    private String password;
}