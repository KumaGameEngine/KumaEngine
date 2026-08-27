//! #version 450

float D_GGX(float NdotH, float roughness) 
{
    float a  = roughness * roughness;
    float a2 = a * a;
    float d  = (NdotH * NdotH) * (a2 - 1.0) + 1.0;
    return a2 / (3.14159265359 * d * d);
}

float G_SchlickGGX(float NdotX, float roughness) 
{
    float r = roughness + 1.0;
    float k = (r * r) / 8.0;
    return NdotX / (NdotX * (1.0 - k) + k);
}
float G_Smith(float NdotV, float NdotL, float roughness) 
{
    return G_SchlickGGX(NdotV, roughness)
         * G_SchlickGGX(NdotL, roughness);
}

vec3 F_Schlick(float cosTheta, vec3 F0) 
{
    return F0 + (1.0 - F0) * exp2((-5.55473 * cosTheta - 6.98316) * cosTheta);
}

vec3 perturbNormal(vec3 N, vec3 T, vec2 uv,texture2D NormalTex, sampler NormalSamp) //! vec3 perturbNormal(vec3 N, vec3 T, vec2 uv)
{
    T = normalize(T - dot(T, N) * N);
    vec3 B   = cross(N, T);
    mat3 TBN = mat3(T, B, N);

    vec3 n = texture(sampler2D(NormalTex, NormalSamp), uv).xyz * 2.0 - 1.0; //! vec3 n;
    return normalize(TBN * n);
}

vec3 pbrLo(float lightRadius, vec3 baseColor, float intensity,
           vec3 Lv, vec3 V, vec3 N, vec3 F0, float NdotV,
           float a2, float k, float denomV, vec3 diffuseAlbedo)
{
    float dist2 = dot(Lv, Lv);
    float lr2 = lightRadius * lightRadius;
    if (dist2 >= lr2) return vec3(0.0);

    vec3 L = Lv * inversesqrt(dist2);
    vec3 H = normalize(V + L);

    float d2_over_r2 = dist2 / lr2;
    float attenuation = clamp(1.0 - d2_over_r2 * d2_over_r2, 0.0, 1.0);
    attenuation = (attenuation * attenuation) / (dist2 + 1e-4);
    vec3 radiance = baseColor * intensity * attenuation;

    float NdotL = max(dot(N, L), 0.0);
    float NdotH = max(dot(N, H), 0.0);
    float HdotV = max(dot(H, V), 0.0);

    float d = NdotH * NdotH * (a2 - 1.0) + 1.0;
    float D = a2 / (3.14159265359 * d * d);

    float denomL = NdotL * (1.0 - k) + k;
    float Vis = 1.0 / (4.0 * denomV * denomL);

    vec3 F = F_Schlick(HdotV, F0);
    vec3 specular = D * Vis * F;
    vec3 diffuse  = (vec3(1.0) - F) * diffuseAlbedo;

    return (diffuse + specular) * radiance * NdotL;
}