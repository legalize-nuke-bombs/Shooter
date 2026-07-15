package com.example.shooter.user;

import jakarta.validation.Valid;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/api/users")
public class UserController {

    private final UserService userService;
    private final UserStatService userStatService;

    public UserController(UserService userService, UserStatService userStatService) {
        this.userService = userService;
        this.userStatService = userStatService;
    }

    @GetMapping("/me")
    public ResponseEntity<UserRepresentation> getMe(@AuthenticationPrincipal Long userId) {
        return ResponseEntity.ok(userService.getById(userId, userId));
    }

    @GetMapping("/by-id/{targetId}")
    public ResponseEntity<UserRepresentation> getById(@AuthenticationPrincipal Long userId, @PathVariable Long targetId) {
        return ResponseEntity.ok(userService.getById(userId, targetId));
    }

    @GetMapping("/by-username/{targetUsername}")
    public ResponseEntity<UserRepresentation> getByUsername(@AuthenticationPrincipal Long userId, @PathVariable String targetUsername) {
        return ResponseEntity.ok(userService.getByUsername(userId, targetUsername));
    }

    @PatchMapping("/me")
    public ResponseEntity<UserRepresentation> patchMe(@AuthenticationPrincipal Long userId, @Valid @RequestBody PatchMeRequest request) {
        return ResponseEntity.ok(userService.patchMe(userId, request));
    }

    @PatchMapping("/me/secure")
    public ResponseEntity<UserRepresentation> patchMeSecure(@AuthenticationPrincipal Long userId, @Valid @RequestBody PatchMeSecureRequest request) {
        return ResponseEntity.ok(userService.patchMeSecure(userId, request));
    }

    @DeleteMapping("/me")
    public ResponseEntity<Void> deleteMe(@AuthenticationPrincipal Long userId, @Valid @RequestBody PasswordRequest request) {
        userService.deleteMe(userId, request);
        return ResponseEntity.noContent().build();
    }

    @GetMapping("/stats")
    public ResponseEntity<UserStatRepresentation> stats() {
        return ResponseEntity.ok(userStatService.get());
    }
}