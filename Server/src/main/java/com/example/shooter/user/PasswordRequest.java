package com.example.shooter.user;

import jakarta.validation.constraints.*;
import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class PasswordRequest {
    @NotNull
    @Size(max = PasswordValidationService.PASSWORD_MAX_LENGTH)
    private String password;
}
