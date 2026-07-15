package com.example.shooter.user;

import jakarta.validation.constraints.*;
import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class PatchMeSecureRequest {
    @NotNull
    @Size(max = PasswordValidationService.PASSWORD_MAX_LENGTH)
    private String currentPassword;

    @Size(min= UserConstants.USERNAME_MIN_LENGTH, max= UserConstants.USERNAME_MAX_LENGTH)
    @Pattern(regexp= UserConstants.USERNAME_PATTERN)
    private String username;

    @Size(min= PasswordValidationService.PASSWORD_MIN_LENGTH, max= PasswordValidationService.PASSWORD_MAX_LENGTH)
    private String password;
}
