package com.example.shooter.jwt;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;
import java.io.IOException;
import java.util.List;

@Component
public class JwtAuthFilter extends OncePerRequestFilter {

    private final JwtTokenProvider jwtTokenProvider;
    private final JwtTokenResolveService jwtTokenResolveService;

    public JwtAuthFilter(JwtTokenProvider jwtTokenProvider, JwtTokenResolveService jwtTokenResolveService) {
        this.jwtTokenProvider = jwtTokenProvider;
        this.jwtTokenResolveService = jwtTokenResolveService;
    }

    @Override
    protected void doFilterInternal(HttpServletRequest request,
                                    HttpServletResponse response,
                                    FilterChain filterChain) throws ServletException, IOException {

        String token = jwtTokenResolveService.resolve(request);

        if (token != null) {
            String payload = jwtTokenProvider.getPayloadFromToken(token).orElse(null);
            Long userId = null;
            try {
                if (payload != null) {
                    userId = Long.parseLong(payload);
                }
            }
            catch (Exception ignored) {}

            if (userId != null) {
                UsernamePasswordAuthenticationToken auth = new UsernamePasswordAuthenticationToken(userId, null, List.of());
                SecurityContextHolder.getContext().setAuthentication(auth);
            }
        }

        filterChain.doFilter(request, response);
    }
}
